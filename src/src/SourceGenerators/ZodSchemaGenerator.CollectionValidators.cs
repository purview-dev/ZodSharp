using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Models;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateCollectionValidations(
		GenerationContext generationContext,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		var displayName = GetDisplayName(generationContext, property);
		var lengthAccessor = ClassifyLengthAccessor(generationContext, propertyType);
		var lengthAttributeData = FindAttribute(attributes, generationContext.LengthAttribute);
		var lengthAttr = lengthAttributeData is null
			? LengthAttributeData.Empty
			: LengthAttributeData.FromAttributeData(generationContext, lengthAttributeData);
		var minLengthAttributeData = FindAttribute(attributes, generationContext.MinLengthAttribute);
		var minLengthAttr = minLengthAttributeData is null
			? MinLengthAttributeData.Empty
			: MinLengthAttributeData.FromAttributeData(generationContext, minLengthAttributeData);
		var maxLengthAttributeData = FindAttribute(attributes, generationContext.MaxLengthAttribute);
		var maxLengthAttr = maxLengthAttributeData is null
			? MaxLengthAttributeData.Empty
			: MaxLengthAttributeData.FromAttributeData(generationContext, maxLengthAttributeData);

		if (!lengthAttr.Exists && !minLengthAttr.Exists && !maxLengthAttr.Exists)
			return;

		if (!lengthAccessor.IsSupported)
		{
			if (lengthAttr.Exists)
				AddUnsupportedLengthTargetDiagnostic(diagnostics, lengthAttributeData, propertyName, propertyType);

			return;
		}

		if (lengthAttr.Exists)
		{
			if (lengthAttr.MinimumLength < 0)
				AddInvalidLengthConfigurationDiagnostic(
					diagnostics,
					lengthAttributeData,
					$"LengthAttribute on '{propertyName}' must use a minimum length greater than or equal to zero."
				);
			else if (lengthAttr.MaximumLength < lengthAttr.MinimumLength)
				AddInvalidLengthConfigurationDiagnostic(
					diagnostics,
					lengthAttributeData,
					$"LengthAttribute on '{propertyName}' must use a maximum length greater than or equal to the minimum length."
				);
		}

		var propertyValueName = GetLocalIdentifier(propertyName, "Value");
		var propertyLengthName = GetLocalIdentifier(propertyName, "Length");
		generationContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
		using (generationContext.Writer.Block($"if ({propertyValueName} is not null)"))
		{
			generationContext.Writer.WriteLine($"var propertyValue = {propertyValueName};");
			generationContext.Writer.WriteLine($"var {propertyLengthName} = {lengthAccessor.LengthExpression};");

			if (
				lengthAttr.Exists
				&& lengthAttr.MinimumLength >= 0
				&& lengthAttr.MaximumLength >= lengthAttr.MinimumLength
			)
			{
				var tooSmallMessage = BuildMessageExpression(
					diagnostics,
					lengthAttributeData,
					displayName,
					lengthAttr.ErrorMessage,
					lengthAttr.ErrorMessageResourceName,
					lengthAttr.ErrorMessageResourceType,
					$"{Quote($"Field '{displayName}' must contain at least ")} + FormatCount({lengthAttr.MinimumLength}, {Quote("element")}, {Quote("elements")}) + {Quote(".")}",
					Quote(displayName),
					lengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
					lengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
				);
				var tooBigMessage = BuildMessageExpression(
					diagnostics,
					lengthAttributeData,
					displayName,
					lengthAttr.ErrorMessage,
					lengthAttr.ErrorMessageResourceName,
					lengthAttr.ErrorMessageResourceType,
					$"{Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({lengthAttr.MaximumLength}, {Quote("element")}, {Quote("elements")}) + {Quote(".")}",
					Quote(displayName),
					lengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
					lengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
				);

				using (generationContext.Writer.Block($"if ({propertyLengthName} < {lengthAttr.MinimumLength})"))
				{
					WriteValidationError(
						generationContext,
						"too_small",
						tooSmallMessage,
						GetPathFieldName(propertyName),
						lengthAccessor.Origin,
						minimum: lengthAttr.MinimumLength
					);
				}

				using (generationContext.Writer.Block($"else if ({propertyLengthName} > {lengthAttr.MaximumLength})"))
				{
					WriteValidationError(
						generationContext,
						"too_big",
						tooBigMessage,
						GetPathFieldName(propertyName),
						lengthAccessor.Origin,
						maximum: lengthAttr.MaximumLength
					);
				}
			}

			if (minLengthAttr.Exists && minLengthAttr.Length > 0)
			{
				var messageExpression = BuildMessageExpression(
					diagnostics,
					minLengthAttributeData,
					displayName,
					minLengthAttr.ErrorMessage,
					minLengthAttr.ErrorMessageResourceName,
					minLengthAttr.ErrorMessageResourceType,
					$"{Quote($"Field '{displayName}' must contain at least ")} + FormatCount({minLengthAttr.Length}, {Quote("element")}, {Quote("elements")}) + {Quote(".")}",
					Quote(displayName),
					minLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				using (generationContext.Writer.Block($"if ({propertyLengthName} < {minLengthAttr.Length})"))
				{
					WriteValidationError(
						generationContext,
						"too_small",
						messageExpression,
						GetPathFieldName(propertyName),
						lengthAccessor.Origin,
						minimum: minLengthAttr.Length
					);
				}
			}

			if (maxLengthAttr.Exists && maxLengthAttr.Length >= 0)
			{
				var messageExpression = BuildMessageExpression(
					diagnostics,
					maxLengthAttributeData,
					displayName,
					maxLengthAttr.ErrorMessage,
					maxLengthAttr.ErrorMessageResourceName,
					maxLengthAttr.ErrorMessageResourceType,
					$"{Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({maxLengthAttr.Length}, {Quote("element")}, {Quote("elements")}) + {Quote(".")}",
					Quote(displayName),
					maxLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				using (generationContext.Writer.Block($"if ({propertyLengthName} > {maxLengthAttr.Length})"))
				{
					WriteValidationError(
						generationContext,
						"too_big",
						messageExpression,
						GetPathFieldName(propertyName),
						lengthAccessor.Origin,
						maximum: maxLengthAttr.Length
					);
				}
			}
		}

		generationContext.Writer.WriteLine();
	}
}
