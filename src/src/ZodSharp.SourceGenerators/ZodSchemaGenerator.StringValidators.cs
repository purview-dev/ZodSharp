using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers.Models;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateStringValidations(
		ExecutionContext executionContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<Diagnostic> diagnostics
	)
	{
		StringLengthValidators(executionContext, property, propertyName, attributes, diagnostics);

		var emailAttribute = EmailAddressAttributeData.FromAttributeData(executionContext, attributes);
		if (emailAttribute.Exists)
		{
			var errorMessage =
				$"global::System.String.Format(global::System.Globalization.CultureInfo.CurrentCulture, {Quote(emailAttribute.ErrorMessage ?? "Field '{0}' must be a valid email address")}, {Quote(GetDisplayName(executionContext, property))})";

			using (
				executionContext.Writer.Block(
					$"if (!global::ZodSharp.Rules.EmailRule.EmailRegex.IsMatch(value.{propertyName}))"
				)
			)
			{
				WriteValidationError(
					executionContext,
					"invalid_string",
					errorMessage,
					GetPathFieldName(propertyName),
					"string"
				);
			}

			executionContext.Writer.WriteLine();
		}

		var regularExpressionAttributeData = FindAttribute(attributes, executionContext.RegularExpressionAttribute);
		var regularExpressionAttribute = regularExpressionAttributeData is null
			? RegularExpressionAttributeData.Empty
			: RegularExpressionAttributeData.FromAttributeData(executionContext, regularExpressionAttributeData);
		if (regularExpressionAttribute.Exists)
		{
			var displayName = GetDisplayName(executionContext, property);
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

			using (executionContext.Writer.Block())
			{
				executionContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				using (
					executionContext.Writer.Block(
						$"if ({propertyValueName}.Length != 0 && !{GetRegexFieldName(propertyName)}.IsMatch({propertyValueName}))"
					)
				)
				{
					WriteValidationError(
						executionContext,
						"invalid_string",
						messageExpression,
						GetPathFieldName(propertyName),
						"string"
					);
				}
			}

			executionContext.Writer.WriteLine();
		}
	}

	static void StringLengthValidators(
		ExecutionContext executionContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<Diagnostic> diagnostics
	)
	{
		var displayName = GetDisplayName(executionContext, property);
		var propertyPath = GetPathFieldName(propertyName);
		var propertyValueName = GetLocalIdentifier(propertyName, "Value");
		var propertyLengthName = GetLocalIdentifier(propertyName, "Length");

		var lengthAttributeData = FindAttribute(attributes, executionContext.LengthAttribute);
		var lengthAttr = lengthAttributeData is null
			? LengthAttributeData.Empty
			: LengthAttributeData.FromAttributeData(executionContext, lengthAttributeData);
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
				using (executionContext.Writer.Block())
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

					executionContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
					using (executionContext.Writer.Block($"if ({propertyValueName} is not null)"))
					{
						executionContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
						using (executionContext.Writer.Block($"if ({propertyLengthName} < {lengthAttr.MinimumLength})"))
						{
							WriteValidationError(
								executionContext,
								"too_small",
								tooSmallMessage,
								propertyPath,
								"string",
								minimum: lengthAttr.MinimumLength
							);
						}

						using (
							executionContext.Writer.Block(
								$"else if ({propertyLengthName} > {lengthAttr.MaximumLength})"
							)
						)
						{
							WriteValidationError(
								executionContext,
								"too_big",
								tooBigMessage,
								propertyPath,
								"string",
								maximum: lengthAttr.MaximumLength
							);
						}
					}
				}

				executionContext.Writer.WriteLine();
			}
		}

		var stringLengthAttributeData = FindAttribute(attributes, executionContext.StringLengthAttribute);
		var stringLengthAttr = stringLengthAttributeData is null
			? StringLengthAttribute.Empty
			: StringLengthAttribute.FromAttributeData(executionContext, stringLengthAttributeData);
		if (stringLengthAttr.Exists)
		{
			using (executionContext.Writer.Block())
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

				executionContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				executionContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				if (stringLengthAttr.MinimumLength > 0)
				{
					using (
						executionContext.Writer.Block($"if ({propertyLengthName} < {stringLengthAttr.MinimumLength})")
					)
					{
						WriteValidationError(
							executionContext,
							"too_small",
							tooSmallMessage,
							propertyPath,
							"string",
							minimum: stringLengthAttr.MinimumLength
						);
					}
				}

				using (executionContext.Writer.Block($"if ({propertyLengthName} > {stringLengthAttr.MaximumLength})"))
				{
					WriteValidationError(
						executionContext,
						"too_big",
						tooBigMessage,
						propertyPath,
						"string",
						maximum: stringLengthAttr.MaximumLength
					);
				}
			}

			executionContext.Writer.WriteLine();
		}

		var minLengthAttributeData = FindAttribute(attributes, executionContext.MinLengthAttribute);
		var minLengthAttr = minLengthAttributeData is null
			? MinLengthAttributeData.Empty
			: MinLengthAttributeData.FromAttributeData(executionContext, minLengthAttributeData);
		if (minLengthAttr.Exists && minLengthAttr.Length > 0)
		{
			using (executionContext.Writer.Block())
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

				executionContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				executionContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				using (executionContext.Writer.Block($"if ({propertyLengthName} < {minLengthAttr.Length})"))
				{
					WriteValidationError(
						executionContext,
						"too_small",
						messageExpression,
						propertyPath,
						"string",
						minimum: minLengthAttr.Length
					);
				}
			}

			executionContext.Writer.WriteLine();
		}

		var maxLengthAttributeData = FindAttribute(attributes, executionContext.MaxLengthAttribute);
		var maxLengthAttr = maxLengthAttributeData is null
			? MaxLengthAttributeData.Empty
			: MaxLengthAttributeData.FromAttributeData(executionContext, maxLengthAttributeData);
		if (maxLengthAttr.Exists && maxLengthAttr.Length >= 0)
		{
			using (executionContext.Writer.Block())
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

				executionContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				executionContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				using (executionContext.Writer.Block($"if ({propertyLengthName} > {maxLengthAttr.Length})"))
				{
					WriteValidationError(
						executionContext,
						"too_big",
						messageExpression,
						propertyPath,
						"string",
						maximum: maxLengthAttr.Length
					);
				}
			}

			executionContext.Writer.WriteLine();
		}
	}
}
