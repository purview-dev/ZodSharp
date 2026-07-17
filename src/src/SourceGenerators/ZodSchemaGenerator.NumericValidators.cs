using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateNumericValidations(
		GenerationContext generationContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		var rangeAttributeData = FindAttribute(attributes, generationContext.RangeAttribute);
		var rangeAttribute = rangeAttributeData is null
			? RangeAttributeData.Empty
			: RangeAttributeData.FromAttributeData(generationContext, rangeAttributeData);
		if (!rangeAttribute.Exists)
			return;

		if (!TryBuildRangeBoundaryExpressions(propertyType, rangeAttribute, out _, out _))
			return;

		var displayName = GetDisplayName(generationContext, property);
		var propertyValueName = GetLocalIdentifier(propertyName, "Value");
		var minComparison = rangeAttribute.MinimumIsExclusive ? "<=" : "<";
		var maxComparison = rangeAttribute.MaximumIsExclusive ? ">=" : ">";
		var minimumDescription = rangeAttribute.MinimumIsExclusive ? "greater than" : "greater than or equal to";
		var maximumDescription = rangeAttribute.MaximumIsExclusive ? "less than" : "less than or equal to";
		var minimumDisplay = Convert.ToString(rangeAttribute.Minimum, CultureInfo.InvariantCulture) ?? string.Empty;
		var maximumDisplay = Convert.ToString(rangeAttribute.Maximum, CultureInfo.InvariantCulture) ?? string.Empty;
		var messageExpression = BuildMessageExpression(
			diagnostics,
			rangeAttributeData,
			displayName,
			rangeAttribute.ErrorMessage,
			rangeAttribute.ErrorMessageResourceName,
			rangeAttribute.ErrorMessageResourceType,
			Quote(
				$"Field '{displayName}' must be {minimumDescription} {minimumDisplay} and {maximumDescription} {maximumDisplay}."
			),
			Quote(displayName),
			Quote(minimumDisplay),
			Quote(maximumDisplay)
		);

		using (generationContext.Writer.Block())
		{
			generationContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				generationContext.Writer.Block(
					$"if ({propertyValueName} {minComparison} {GetRangeMinimumFieldName(propertyName)} || {propertyValueName} {maxComparison} {GetRangeMaximumFieldName(propertyName)})"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_range",
					messageExpression,
					GetPathFieldName(propertyName)
				);
			}
		}

		generationContext.Writer.WriteLine();
	}

	static bool TryBuildRangeBoundaryExpressions(
		ITypeSymbol propertyType,
		RangeAttributeData rangeAttribute,
		out string minimumExpression,
		out string maximumExpression
	)
	{
		propertyType = TypeHelpers.UnwrapNullableType(propertyType);
		if (TypeHelpers.IsNumericType(propertyType))
			return TryBuildNumericRangeBoundaryExpressions(
				propertyType,
				rangeAttribute,
				out minimumExpression,
				out maximumExpression
			);

		if (
			TypeHelpers.IsNamedType(propertyType, "System.DateTime")
			&& rangeAttribute.Kind == RangeAttributeKind.Converted
		)
		{
			minimumExpression = BuildDateTimeParseExpression(
				(string)rangeAttribute.Minimum!,
				rangeAttribute.ParseLimitsInInvariantCulture
			);
			maximumExpression = BuildDateTimeParseExpression(
				(string)rangeAttribute.Maximum!,
				rangeAttribute.ParseLimitsInInvariantCulture
			);
			return true;
		}

		if (
			TypeHelpers.IsNamedType(propertyType, "System.DateOnly")
			&& rangeAttribute.Kind == RangeAttributeKind.Converted
		)
		{
			minimumExpression = BuildDateOnlyParseExpression(
				(string)rangeAttribute.Minimum!,
				rangeAttribute.ParseLimitsInInvariantCulture
			);
			maximumExpression = BuildDateOnlyParseExpression(
				(string)rangeAttribute.Maximum!,
				rangeAttribute.ParseLimitsInInvariantCulture
			);
			return true;
		}

		if (
			TypeHelpers.IsNamedType(propertyType, "System.TimeOnly")
			&& rangeAttribute.Kind == RangeAttributeKind.Converted
		)
		{
			minimumExpression = BuildTimeOnlyParseExpression(
				(string)rangeAttribute.Minimum!,
				rangeAttribute.ParseLimitsInInvariantCulture
			);
			maximumExpression = BuildTimeOnlyParseExpression(
				(string)rangeAttribute.Maximum!,
				rangeAttribute.ParseLimitsInInvariantCulture
			);
			return true;
		}

		minimumExpression = string.Empty;
		maximumExpression = string.Empty;
		return false;
	}

	static bool TryBuildNumericRangeBoundaryExpressions(
		ITypeSymbol propertyType,
		RangeAttributeData rangeAttribute,
		out string minimumExpression,
		out string maximumExpression
	)
	{
		if (rangeAttribute.Kind == RangeAttributeKind.Int32)
		{
			minimumExpression = ConvertNumericLiteralExpression(propertyType, (int)rangeAttribute.Minimum!);
			maximumExpression = ConvertNumericLiteralExpression(propertyType, (int)rangeAttribute.Maximum!);
			return minimumExpression.Length > 0 && maximumExpression.Length > 0;
		}

		if (rangeAttribute.Kind == RangeAttributeKind.Double)
		{
			minimumExpression = ConvertNumericLiteralExpression(propertyType, (double)rangeAttribute.Minimum!);
			maximumExpression = ConvertNumericLiteralExpression(propertyType, (double)rangeAttribute.Maximum!);
			return minimumExpression.Length > 0 && maximumExpression.Length > 0;
		}

		if (
			rangeAttribute.Kind == RangeAttributeKind.Converted
			&& rangeAttribute.Minimum is string minimum
			&& rangeAttribute.Maximum is string maximum
		)
		{
			minimumExpression = BuildNumericParseExpression(
				propertyType,
				minimum,
				rangeAttribute.ParseLimitsInInvariantCulture
			);
			maximumExpression = BuildNumericParseExpression(
				propertyType,
				maximum,
				rangeAttribute.ParseLimitsInInvariantCulture
			);
			return minimumExpression.Length > 0 && maximumExpression.Length > 0;
		}

		minimumExpression = string.Empty;
		maximumExpression = string.Empty;
		return false;
	}

	static string ConvertNumericLiteralExpression(ITypeSymbol propertyType, int value) =>
		propertyType.SpecialType switch
		{
			SpecialType.System_Byte => $"(byte){value.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_SByte => $"(sbyte){value.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_Int16 => $"(short){value.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_UInt16 => $"(ushort){value.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_Int32 => value.ToString(CultureInfo.InvariantCulture),
			SpecialType.System_UInt32 => $"{value.ToString(CultureInfo.InvariantCulture)}U",
			SpecialType.System_Int64 => $"{value.ToString(CultureInfo.InvariantCulture)}L",
			SpecialType.System_UInt64 => $"{value.ToString(CultureInfo.InvariantCulture)}UL",
			SpecialType.System_Single => value.ToString(CultureInfo.InvariantCulture) + "F",
			SpecialType.System_Double => value.ToString(CultureInfo.InvariantCulture) + "D",
			SpecialType.System_Decimal => value.ToString(CultureInfo.InvariantCulture) + "M",
			_ => string.Empty,
		};

	static string ConvertNumericLiteralExpression(ITypeSymbol propertyType, double value) =>
		propertyType.SpecialType switch
		{
			SpecialType.System_Single => $"(float){value.ToString("R", CultureInfo.InvariantCulture)}D",
			SpecialType.System_Double => value.ToString("R", CultureInfo.InvariantCulture) + "D",
			SpecialType.System_Decimal => $"(decimal){value.ToString("R", CultureInfo.InvariantCulture)}D",
			_ => BuildNumericParseExpression(
				propertyType,
				value.ToString("R", CultureInfo.InvariantCulture),
				invariantCulture: true
			),
		};

	static string BuildNumericParseExpression(ITypeSymbol propertyType, string value, bool invariantCulture)
	{
		var cultureExpression = invariantCulture
			? "global::System.Globalization.CultureInfo.InvariantCulture"
			: "global::System.Globalization.CultureInfo.CurrentCulture";

		return propertyType.SpecialType switch
		{
			SpecialType.System_Byte =>
				$"global::System.Byte.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Integer, {cultureExpression})",
			SpecialType.System_SByte =>
				$"global::System.SByte.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Integer, {cultureExpression})",
			SpecialType.System_Int16 =>
				$"global::System.Int16.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Integer, {cultureExpression})",
			SpecialType.System_UInt16 =>
				$"global::System.UInt16.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Integer, {cultureExpression})",
			SpecialType.System_Int32 =>
				$"global::System.Int32.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Integer, {cultureExpression})",
			SpecialType.System_UInt32 =>
				$"global::System.UInt32.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Integer, {cultureExpression})",
			SpecialType.System_Int64 =>
				$"global::System.Int64.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Integer, {cultureExpression})",
			SpecialType.System_UInt64 =>
				$"global::System.UInt64.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Integer, {cultureExpression})",
			SpecialType.System_Single =>
				$"global::System.Single.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Float | global::System.Globalization.NumberStyles.AllowThousands, {cultureExpression})",
			SpecialType.System_Double =>
				$"global::System.Double.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Float | global::System.Globalization.NumberStyles.AllowThousands, {cultureExpression})",
			SpecialType.System_Decimal =>
				$"global::System.Decimal.Parse({Quote(value)}, global::System.Globalization.NumberStyles.Number, {cultureExpression})",
			_ => string.Empty,
		};
	}

	static string BuildDateTimeParseExpression(string value, bool invariantCulture)
	{
		var cultureExpression = invariantCulture
			? "global::System.Globalization.CultureInfo.InvariantCulture"
			: "global::System.Globalization.CultureInfo.CurrentCulture";
		return $"global::System.DateTime.Parse({Quote(value)}, {cultureExpression})";
	}

	static string BuildDateOnlyParseExpression(string value, bool invariantCulture)
	{
		var cultureExpression = invariantCulture
			? "global::System.Globalization.CultureInfo.InvariantCulture"
			: "global::System.Globalization.CultureInfo.CurrentCulture";
		return $"global::System.DateOnly.Parse({Quote(value)}, {cultureExpression})";
	}

	static string BuildTimeOnlyParseExpression(string value, bool invariantCulture)
	{
		var cultureExpression = invariantCulture
			? "global::System.Globalization.CultureInfo.InvariantCulture"
			: "global::System.Globalization.CultureInfo.CurrentCulture";
		return $"global::System.TimeOnly.Parse({Quote(value)}, {cultureExpression})";
	}
}
