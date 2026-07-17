using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Models;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateStringValidations(
		GenerationContext generationContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		StringLengthValidators(generationContext, property, propertyName, attributes, diagnostics);

		var emailAttribute = EmailAddressAttributeData.FromAttributeData(generationContext, attributes);
		if (emailAttribute.Exists)
		{
			var errorMessage =
				$"global::System.String.Format(global::System.Globalization.CultureInfo.CurrentCulture, {Quote(emailAttribute.ErrorMessage ?? "Field '{0}' must be a valid email address")}, {Quote(GetDisplayName(generationContext, property))})";

			using (
				generationContext.Writer.Block(
					$"if (!global::ZodSharp.Rules.EmailRule.EmailRegex.IsMatch(value.{propertyName}))"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_string",
					errorMessage,
					GetPathFieldName(propertyName),
					"string"
				);
			}

			generationContext.Writer.WriteLine();
		}

		var regularExpressionAttributeData = FindAttribute(attributes, generationContext.RegularExpressionAttribute);
		var regularExpressionAttribute = regularExpressionAttributeData is null
			? RegularExpressionAttributeData.Empty
			: RegularExpressionAttributeData.FromAttributeData(generationContext, regularExpressionAttributeData);
		if (regularExpressionAttribute.Exists)
		{
			var displayName = GetDisplayName(generationContext, property);
			var propertyValueName = GetLocalIdentifier(propertyName, "Value");
			var messageExpression = BuildMessageExpression(
				diagnostics,
				regularExpressionAttributeData,
				displayName,
				regularExpressionAttribute.ErrorMessage,
				regularExpressionAttribute.ErrorMessageResourceName,
				regularExpressionAttribute.ErrorMessageResourceType,
				Quote(
					$"Field '{displayName}' must match the regular expression '{regularExpressionAttribute.Pattern}'."
				),
				Quote(displayName),
				Quote(regularExpressionAttribute.Pattern ?? string.Empty)
			);

			using (generationContext.Writer.Block())
			{
				generationContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				using (
					generationContext.Writer.Block(
						$"if ({propertyValueName}.Length != 0 && !{GetRegexFieldName(propertyName)}.IsMatch({propertyValueName}))"
					)
				)
				{
					WriteValidationError(
						generationContext,
						"invalid_string",
						messageExpression,
						GetPathFieldName(propertyName),
						"string"
					);
				}
			}

			generationContext.Writer.WriteLine();
		}
	}

	static void StringLengthValidators(
		GenerationContext generationContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		var displayName = GetDisplayName(generationContext, property);
		var propertyPath = GetPathFieldName(propertyName);
		var propertyValueName = GetLocalIdentifier(propertyName, "Value");
		var propertyLengthName = GetLocalIdentifier(propertyName, "Length");

		var lengthAttributeData = FindAttribute(attributes, generationContext.LengthAttribute);
		var lengthAttr = lengthAttributeData is null
			? LengthAttributeData.Empty
			: LengthAttributeData.FromAttributeData(generationContext, lengthAttributeData);
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
			else
			{
				using (generationContext.Writer.Block())
				{
					var tooSmallMessage = BuildMessageExpression(
						diagnostics,
						lengthAttributeData,
						displayName,
						lengthAttr.ErrorMessage,
						lengthAttr.ErrorMessageResourceName,
						lengthAttr.ErrorMessageResourceType,
						$"{Quote($"Field '{displayName}' must contain at least ")} + FormatCount({lengthAttr.MinimumLength}, {Quote("character")}, {Quote("characters")}) + {Quote(".")}",
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
						$"{Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({lengthAttr.MaximumLength}, {Quote("character")}, {Quote("characters")}) + {Quote(".")}",
						Quote(displayName),
						lengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
						lengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
					);

					generationContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
					using (generationContext.Writer.Block($"if ({propertyValueName} is not null)"))
					{
						generationContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
						using (
							generationContext.Writer.Block($"if ({propertyLengthName} < {lengthAttr.MinimumLength})")
						)
						{
							WriteValidationError(
								generationContext,
								"too_small",
								tooSmallMessage,
								propertyPath,
								"string",
								minimum: lengthAttr.MinimumLength
							);
						}

						using (
							generationContext.Writer.Block(
								$"else if ({propertyLengthName} > {lengthAttr.MaximumLength})"
							)
						)
						{
							WriteValidationError(
								generationContext,
								"too_big",
								tooBigMessage,
								propertyPath,
								"string",
								maximum: lengthAttr.MaximumLength
							);
						}
					}
				}

				generationContext.Writer.WriteLine();
			}
		}

		var stringLengthAttributeData = FindAttribute(attributes, generationContext.StringLengthAttribute);
		var stringLengthAttr = stringLengthAttributeData is null
			? StringLengthAttribute.Empty
			: StringLengthAttribute.FromAttributeData(generationContext, stringLengthAttributeData);
		if (stringLengthAttr.Exists)
		{
			using (generationContext.Writer.Block())
			{
				var tooSmallMessage = BuildMessageExpression(
					diagnostics,
					stringLengthAttributeData,
					displayName,
					stringLengthAttr.ErrorMessage,
					stringLengthAttr.ErrorMessageResourceName,
					stringLengthAttr.ErrorMessageResourceType,
					$"{Quote($"Field '{displayName}' must contain at least ")} + FormatCount({stringLengthAttr.MinimumLength}, {Quote("character")}, {Quote("characters")}) + {Quote(".")}",
					Quote(displayName),
					stringLengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
					stringLengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
				);
				var tooBigMessage = BuildMessageExpression(
					diagnostics,
					stringLengthAttributeData,
					displayName,
					stringLengthAttr.ErrorMessage,
					stringLengthAttr.ErrorMessageResourceName,
					stringLengthAttr.ErrorMessageResourceType,
					$"{Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({stringLengthAttr.MaximumLength}, {Quote("character")}, {Quote("characters")}) + {Quote(".")}",
					Quote(displayName),
					stringLengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
					stringLengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
				);

				generationContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				generationContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				if (stringLengthAttr.MinimumLength > 0)
				{
					using (
						generationContext.Writer.Block($"if ({propertyLengthName} < {stringLengthAttr.MinimumLength})")
					)
					{
						WriteValidationError(
							generationContext,
							"too_small",
							tooSmallMessage,
							propertyPath,
							"string",
							minimum: stringLengthAttr.MinimumLength
						);
					}
				}

				using (generationContext.Writer.Block($"if ({propertyLengthName} > {stringLengthAttr.MaximumLength})"))
				{
					WriteValidationError(
						generationContext,
						"too_big",
						tooBigMessage,
						propertyPath,
						"string",
						maximum: stringLengthAttr.MaximumLength
					);
				}
			}

			generationContext.Writer.WriteLine();
		}

		var minLengthAttributeData = FindAttribute(attributes, generationContext.MinLengthAttribute);
		var minLengthAttr = minLengthAttributeData is null
			? MinLengthAttributeData.Empty
			: MinLengthAttributeData.FromAttributeData(generationContext, minLengthAttributeData);
		if (minLengthAttr.Exists && minLengthAttr.Length > 0)
		{
			using (generationContext.Writer.Block())
			{
				var messageExpression = BuildMessageExpression(
					diagnostics,
					minLengthAttributeData,
					displayName,
					minLengthAttr.ErrorMessage,
					minLengthAttr.ErrorMessageResourceName,
					minLengthAttr.ErrorMessageResourceType,
					$"{Quote($"Field '{displayName}' must contain at least ")} + FormatCount({minLengthAttr.Length}, {Quote("character")}, {Quote("characters")}) + {Quote(".")}",
					Quote(displayName),
					minLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				generationContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				generationContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				using (generationContext.Writer.Block($"if ({propertyLengthName} < {minLengthAttr.Length})"))
				{
					WriteValidationError(
						generationContext,
						"too_small",
						messageExpression,
						propertyPath,
						"string",
						minimum: minLengthAttr.Length
					);
				}
			}

			generationContext.Writer.WriteLine();
		}

		var maxLengthAttributeData = FindAttribute(attributes, generationContext.MaxLengthAttribute);
		var maxLengthAttr = maxLengthAttributeData is null
			? MaxLengthAttributeData.Empty
			: MaxLengthAttributeData.FromAttributeData(generationContext, maxLengthAttributeData);
		if (maxLengthAttr.Exists && maxLengthAttr.Length >= 0)
		{
			using (generationContext.Writer.Block())
			{
				var messageExpression = BuildMessageExpression(
					diagnostics,
					maxLengthAttributeData,
					displayName,
					maxLengthAttr.ErrorMessage,
					maxLengthAttr.ErrorMessageResourceName,
					maxLengthAttr.ErrorMessageResourceType,
					$"{Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({maxLengthAttr.Length}, {Quote("character")}, {Quote("characters")}) + {Quote(".")}",
					Quote(displayName),
					maxLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				generationContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				generationContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				using (generationContext.Writer.Block($"if ({propertyLengthName} > {maxLengthAttr.Length})"))
				{
					WriteValidationError(
						generationContext,
						"too_big",
						messageExpression,
						propertyPath,
						"string",
						maximum: maxLengthAttr.Length
					);
				}
			}

			generationContext.Writer.WriteLine();
		}
	}
}
