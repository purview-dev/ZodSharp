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
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		GenerateAllowedValuesValidation(outputContext, property, propertyType, propertyName, attributes, diagnostics);
		GenerateDeniedValuesValidation(outputContext, property, propertyType, propertyName, attributes, diagnostics);
	}

	static void GenerateAllowedValuesValidation(
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		outputContext.Debug(
			$"Generating AllowedValues validation for property '{propertyName}' of type '{propertyType.ToDisplayString()}'",
			1
		);

		var allowedValues = AllowedValuesAttributeData.FromAttributeData(attributes, out var attributeData);
		if (!allowedValues.Exists)
			return;

		if (
			!TryBuildValueSetComparison(
				outputContext,
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
			outputContext,
			diagnostics,
			attributeData,
			displayName,
			allowedValues.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' must be one of the following values: {displayValues}."),
			CodeGenHelpers.Quote(displayName),
			CodeGenHelpers.Quote(displayValues)
		);

		using (outputContext.Writer.OpenBlockScope())
		{
			outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				outputContext.Writer.OpenBlockScope(
					$"if (!({comparisonExpression.Replace("propertyValue", propertyValueName)}))"
				)
			)
			{
				WriteValidationError(
					outputContext,
					"invalid_value",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName)
				);
			}
		}

		outputContext.Writer.WriteLine();
	}

	static void GenerateDeniedValuesValidation(
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		outputContext.Debug(
			$"Generating DeniedValues validation for property '{propertyName}' of type '{propertyType.ToDisplayString()}'",
			1
		);

		var deniedValues = DeniedValuesAttributeData.FromAttributeData(attributes, out var attributeData);
		if (!deniedValues.Exists)
			return;

		if (
			!TryBuildValueSetComparison(
				outputContext,
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
			outputContext,
			diagnostics,
			attributeData,
			displayName,
			deniedValues.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' contains a denied value. Disallowed values: {displayValues}."),
			CodeGenHelpers.Quote(displayName),
			CodeGenHelpers.Quote(displayValues)
		);

		using (outputContext.Writer.OpenBlockScope())
		{
			outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				outputContext.Writer.OpenBlockScope(
					$"if ({comparisonExpression.Replace("propertyValue", propertyValueName)})"
				)
			)
			{
				WriteValidationError(
					outputContext,
					"invalid_value",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName)
				);
			}
		}

		outputContext.Writer.WriteLine();
	}

	static bool TryBuildValueSetComparison(
		SchemaGenerationOutputContext outputContext,
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
		outputContext.Debug(
			$"Building value set comparison for property '{propertyName}' of type '{propertyType.ToDisplayString()}'",
			1
		);

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
