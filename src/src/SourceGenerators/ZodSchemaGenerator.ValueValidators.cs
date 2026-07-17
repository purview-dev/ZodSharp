using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateValueSetValidations(
		GenerationContext generationContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		GenerateAllowedValuesValidation(
			generationContext,
			property,
			propertyType,
			propertyName,
			attributes,
			diagnostics
		);
		GenerateDeniedValuesValidation(generationContext, property, propertyType, propertyName, attributes, diagnostics);
	}

	static void GenerateAllowedValuesValidation(
		GenerationContext generationContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		var attributeData = FindAttribute(attributes, generationContext.AllowedValuesAttribute);
		var allowedValues = attributeData is null
			? AllowedValuesAttributeData.Empty
			: AllowedValuesAttributeData.FromAttributeData(generationContext, attributeData);
		if (!allowedValues.Exists)
			return;

		if (
			!TryBuildValueSetComparison(
				property.Type,
				propertyName,
				allowedValues.Values,
				out var comparisonExpression,
				out var displayValues,
				diagnostics,
				attributeData,
				property.Name,
				"AllowedValuesAttribute"
			)
		)
		{
			return;
		}

		var propertyValueName = GetLocalIdentifier(propertyName, "Value");
		var displayName = GetDisplayName(generationContext, property);
		var messageExpression = BuildMessageExpression(
			diagnostics,
			attributeData,
			displayName,
			allowedValues.ErrorMessage,
			allowedValues.ErrorMessageResourceName,
			allowedValues.ErrorMessageResourceType,
			Quote($"Field '{displayName}' must be one of the following values: {displayValues}."),
			Quote(displayName),
			Quote(displayValues)
		);

		using (generationContext.Writer.Block())
		{
			generationContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				generationContext.Writer.Block(
					$"if (!({comparisonExpression.Replace("propertyValue", propertyValueName)}))"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_value",
					messageExpression,
					GetPathFieldName(propertyName)
				);
			}
		}

		generationContext.Writer.WriteLine();
	}

	static void GenerateDeniedValuesValidation(
		GenerationContext generationContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		var attributeData = FindAttribute(attributes, generationContext.DeniedValuesAttribute);
		var deniedValues = attributeData is null
			? DeniedValuesAttributeData.Empty
			: DeniedValuesAttributeData.FromAttributeData(generationContext, attributeData);
		if (!deniedValues.Exists)
			return;

		if (
			!TryBuildValueSetComparison(
				property.Type,
				propertyName,
				deniedValues.Values,
				out var comparisonExpression,
				out var displayValues,
				diagnostics,
				attributeData,
				property.Name,
				"DeniedValuesAttribute"
			)
		)
		{
			return;
		}

		var propertyValueName = GetLocalIdentifier(propertyName, "Value");
		var displayName = GetDisplayName(generationContext, property);
		var messageExpression = BuildMessageExpression(
			diagnostics,
			attributeData,
			displayName,
			deniedValues.ErrorMessage,
			deniedValues.ErrorMessageResourceName,
			deniedValues.ErrorMessageResourceType,
			Quote($"Field '{displayName}' contains a denied value. Disallowed values: {displayValues}."),
			Quote(displayName),
			Quote(displayValues)
		);

		using (generationContext.Writer.Block())
		{
			generationContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				generationContext.Writer.Block(
					$"if ({comparisonExpression.Replace("propertyValue", propertyValueName)})"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_value",
					messageExpression,
					GetPathFieldName(propertyName)
				);
			}
		}

		generationContext.Writer.WriteLine();
	}

	static bool TryBuildValueSetComparison(
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<TypedConstant> values,
		out string comparisonExpression,
		out string displayValues,
		List<DiagnosticInfo> diagnostics,
		AttributeData? attributeData,
		string memberName,
		string attributeName
	)
	{
		var comparisons = new List<string>(values.Length);
		var propertyCanBeNull = TypeHelpers.CanBeNull(propertyType);
		var normalizedPropertyType = TypeHelpers.UnwrapNullableType(propertyType);

		for (var i = 0; i < values.Length; i++)
		{
			if (!TryBuildTypedConstantExpression(values[i], propertyType, propertyCanBeNull, out var expression, out _))
			{
				AddUnsupportedDataAnnotationsDiagnostic(
					diagnostics,
					attributeData,
					string.Format(
						CultureInfo.InvariantCulture,
						"{0} on '{1}' contains a value that ZodSharp cannot represent safely for '{2}'.",
						attributeName,
						memberName,
						normalizedPropertyType.ToDisplayString()
					)
				);
				comparisonExpression = string.Empty;
				displayValues = string.Empty;
				return false;
			}

			comparisons.Add(BuildEqualityComparisonExpression(propertyType, "propertyValue", expression));
		}

		comparisonExpression = comparisons.Count == 0 ? "false" : string.Join(" || ", comparisons);
		displayValues = BuildValueListDisplay(values);
		return true;
	}
}
