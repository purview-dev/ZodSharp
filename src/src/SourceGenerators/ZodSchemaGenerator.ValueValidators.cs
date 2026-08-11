using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateValueSetValidations(
		GenerationContext generationContext,
		GenerationLogger? logger,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		GenerateAllowedValuesValidation(
			generationContext,
			logger,
			property,
			propertyType,
			propertyName,
			attributes,
			diagnostics
		);
		GenerateDeniedValuesValidation(
			generationContext,
			logger,
			property,
			propertyType,
			propertyName,
			attributes,
			diagnostics
		);
	}

	static void GenerateAllowedValuesValidation(
		GenerationContext generationContext,
		GenerationLogger? logger,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		logger?.Debug(
			$"Generating AllowedValues validation for property '{propertyName}' of type '{propertyType.ToDisplayString()}'",
			1
		);

		var allowedValues = AllowedValuesAttributeData.FromAttributeData(
			attributes,
			out var attributeData
		);
		if (!allowedValues.Exists)
			return;

		if (
			!TryBuildValueSetComparison(
				logger,
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

		var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
		var displayName = GetDisplayName(property);
		var messageExpression = BuildMessageExpression(
			logger,
			diagnostics,
			attributeData,
			displayName,
			allowedValues.ValidationAttribute,
			CodeGenHelpers.Quote(
				$"Field '{displayName}' must be one of the following values: {displayValues}."
			),
			CodeGenHelpers.Quote(displayName),
			CodeGenHelpers.Quote(displayValues)
		);

		using (generationContext.CodeWriter.Block())
		{
			generationContext.CodeWriter.WriteLine(
				$"var {propertyValueName} = value.{propertyName};"
			);
			using (
				generationContext.CodeWriter.Block(
					$"if (!({comparisonExpression.Replace("propertyValue", propertyValueName)}))"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_value",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName)
				);
			}
		}

		generationContext.CodeWriter.WriteLine();
	}

	static void GenerateDeniedValuesValidation(
		GenerationContext generationContext,
		GenerationLogger? logger,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		logger?.Debug(
			$"Generating DeniedValues validation for property '{propertyName}' of type '{propertyType.ToDisplayString()}'",
			1
		);

		var deniedValues = DeniedValuesAttributeData.FromAttributeData(
			attributes,
			out var attributeData
		);
		if (!deniedValues.Exists)
			return;

		if (
			!TryBuildValueSetComparison(
				logger,
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

		var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
		var displayName = GetDisplayName(property);
		var messageExpression = BuildMessageExpression(
			logger,
			diagnostics,
			attributeData,
			displayName,
			deniedValues.ValidationAttribute,
			CodeGenHelpers.Quote(
				$"Field '{displayName}' contains a denied value. Disallowed values: {displayValues}."
			),
			CodeGenHelpers.Quote(displayName),
			CodeGenHelpers.Quote(displayValues)
		);

		using (generationContext.CodeWriter.Block())
		{
			generationContext.CodeWriter.WriteLine(
				$"var {propertyValueName} = value.{propertyName};"
			);
			using (
				generationContext.CodeWriter.Block(
					$"if ({comparisonExpression.Replace("propertyValue", propertyValueName)})"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_value",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName)
				);
			}
		}

		generationContext.CodeWriter.WriteLine();
	}

	static bool TryBuildValueSetComparison(
		GenerationLogger? logger,
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
		logger?.Debug(
			$"Building value set comparison for property '{propertyName}' of type '{propertyType.ToDisplayString()}'",
			1
		);

		var comparisons = new List<string>(values.Length);
		var propertyCanBeNull = TypeHelpers.CanBeNull(propertyType);
		var normalizedPropertyType = TypeHelpers.UnwrapNullableType(propertyType);

		for (var i = 0; i < values.Length; i++)
		{
			if (
				!TryBuildTypedConstantExpression(
					values[i],
					propertyType,
					propertyCanBeNull,
					out var expression,
					out _
				)
			)
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

			comparisons.Add(
				BuildEqualityComparisonExpression(propertyType, "propertyValue", expression)
			);
		}

		comparisonExpression = comparisons.Count == 0 ? "false" : string.Join(" || ", comparisons);
		displayValues = BuildValueListDisplay(values);
		return true;
	}
}
