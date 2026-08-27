using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateCollectionValidations(
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		var displayName = GetDisplayName(property);
		var lengthAccessor = ClassifyLengthAccessor(propertyType);
		var lengthAttr = LengthAttributeData.FromAttributeData(attributes, out var lengthAttrData);
		var minLengthAttr = MinLengthAttributeData.FromAttributeData(attributes, out var minLengthAttrData);
		var maxLengthAttr = MaxLengthAttributeData.FromAttributeData(attributes, out var maxLengthAttrData);

		if (!lengthAttr.Exists && !minLengthAttr.Exists && !maxLengthAttr.Exists)
			return;

		if (!lengthAccessor.IsSupported)
		{
			if (lengthAttr.Exists)
			{
				AddUnsupportedLengthTargetDiagnostic(diagnostics, lengthAttrData, propertyName, propertyType);
			}

			return;
		}

		if (lengthAttr.Exists)
		{
			if (lengthAttr.MinimumLength < 0)
				AddInvalidLengthConfigurationDiagnostic(
					diagnostics,
					lengthAttrData,
					$"LengthAttribute on '{propertyName}' must use a minimum length greater than or equal to zero."
				);
			else if (lengthAttr.MaximumLength < lengthAttr.MinimumLength)
				AddInvalidLengthConfigurationDiagnostic(
					diagnostics,
					lengthAttrData,
					$"LengthAttribute on '{propertyName}' must use a maximum length greater than or equal to the minimum length."
				);
		}

		var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
		var propertyLengthName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Length");
		outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
		using (outputContext.Writer.OpenBlockScope($"if ({propertyValueName} is not null)"))
		{
			outputContext.Writer.WriteLine($"var propertyValue = {propertyValueName};");
			outputContext.Writer.WriteLine($"var {propertyLengthName} = {lengthAccessor.LengthExpression};");

			if (
				lengthAttr.Exists
				&& lengthAttr.MinimumLength >= 0
				&& lengthAttr.MaximumLength >= lengthAttr.MinimumLength
			)
			{
				var tooSmallMessage = BuildMessageExpression(
					outputContext,
					diagnostics,
					lengthAttrData,
					displayName,
					lengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain at least ")} + FormatCount({lengthAttr.MinimumLength}, {CodeGenHelpers.Quote("element")}, {CodeGenHelpers.Quote("elements")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					lengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
					lengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
				);
				var tooBigMessage = BuildMessageExpression(
					outputContext,
					diagnostics,
					lengthAttrData,
					displayName,
					lengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({lengthAttr.MaximumLength}, {CodeGenHelpers.Quote("element")}, {CodeGenHelpers.Quote("elements")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					lengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
					lengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
				);

				using (outputContext.Writer.OpenBlockScope($"if ({propertyLengthName} < {lengthAttr.MinimumLength})"))
				{
					WriteValidationError(
						outputContext,
						"too_small",
						tooSmallMessage,
						CodeGenHelpers.GetPathFieldName(propertyName),
						lengthAccessor.Origin,
						minimum: lengthAttr.MinimumLength
					);
				}

				using (
					outputContext.Writer.OpenBlockScope($"else if ({propertyLengthName} > {lengthAttr.MaximumLength})")
				)
				{
					WriteValidationError(
						outputContext,
						"too_big",
						tooBigMessage,
						CodeGenHelpers.GetPathFieldName(propertyName),
						lengthAccessor.Origin,
						maximum: lengthAttr.MaximumLength
					);
				}
			}

			if (minLengthAttr.Exists && minLengthAttr.Length > 0)
			{
				var messageExpression = BuildMessageExpression(
					outputContext,
					diagnostics,
					minLengthAttrData,
					displayName,
					minLengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain at least ")} + FormatCount({minLengthAttr.Length}, {CodeGenHelpers.Quote("element")}, {CodeGenHelpers.Quote("elements")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					minLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				using (outputContext.Writer.OpenBlockScope($"if ({propertyLengthName} < {minLengthAttr.Length})"))
				{
					WriteValidationError(
						outputContext,
						"too_small",
						messageExpression,
						CodeGenHelpers.GetPathFieldName(propertyName),
						lengthAccessor.Origin,
						minimum: minLengthAttr.Length
					);
				}
			}

			if (maxLengthAttr.Exists && maxLengthAttr.Length >= 0)
			{
				var messageExpression = BuildMessageExpression(
					outputContext,
					diagnostics,
					maxLengthAttrData,
					displayName,
					maxLengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({maxLengthAttr.Length}, {CodeGenHelpers.Quote("element")}, {CodeGenHelpers.Quote("elements")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					maxLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				using (outputContext.Writer.OpenBlockScope($"if ({propertyLengthName} > {maxLengthAttr.Length})"))
				{
					WriteValidationError(
						outputContext,
						"too_big",
						messageExpression,
						CodeGenHelpers.GetPathFieldName(propertyName),
						lengthAccessor.Origin,
						maximum: maxLengthAttr.Length
					);
				}
			}
		}

		outputContext.Writer.WriteLine();
	}
}
