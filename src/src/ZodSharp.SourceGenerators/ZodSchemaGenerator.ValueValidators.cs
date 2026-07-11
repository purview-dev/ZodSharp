using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Helpers.Models;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateValueSetValidations(
		ExecutionContext executionContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<Diagnostic> diagnostics
	)
	{
		GenerateAllowedValuesValidation(
			executionContext,
			property,
			propertyType,
			propertyName,
			attributes,
			diagnostics
		);
		GenerateDeniedValuesValidation(executionContext, property, propertyType, propertyName, attributes, diagnostics);
	}

	static void GenerateAllowedValuesValidation(
		ExecutionContext executionContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<Diagnostic> diagnostics
	)
	{
		var attributeData = FindAttribute(attributes, executionContext.AllowedValuesAttribute);
		var allowedValues = attributeData is null
			? AllowedValuesAttributeData.Empty
			: AllowedValuesAttributeData.FromAttributeData(executionContext, attributeData);
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
		var displayName = GetDisplayName(executionContext, property);
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

		using (executionContext.Writer.Block())
		{
			executionContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				executionContext.Writer.Block(
					$"if (!({comparisonExpression.Replace("propertyValue", propertyValueName)}))"
				)
			)
			{
				WriteValidationError(
					executionContext,
					"invalid_value",
					messageExpression,
					GetPathFieldName(propertyName)
				);
			}
		}

		executionContext.Writer.WriteLine();
	}

	static void GenerateDeniedValuesValidation(
		ExecutionContext executionContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<Diagnostic> diagnostics
	)
	{
		var attributeData = FindAttribute(attributes, executionContext.DeniedValuesAttribute);
		var deniedValues = attributeData is null
			? DeniedValuesAttributeData.Empty
			: DeniedValuesAttributeData.FromAttributeData(executionContext, attributeData);
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
		var displayName = GetDisplayName(executionContext, property);
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

		using (executionContext.Writer.Block())
		{
			executionContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				executionContext.Writer.Block(
					$"if ({comparisonExpression.Replace("propertyValue", propertyValueName)})"
				)
			)
			{
				WriteValidationError(
					executionContext,
					"invalid_value",
					messageExpression,
					GetPathFieldName(propertyName)
				);
			}
		}

		executionContext.Writer.WriteLine();
	}

	static bool TryBuildValueSetComparison(
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<TypedConstant> values,
		out string comparisonExpression,
		out string displayValues,
		List<Diagnostic> diagnostics,
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
