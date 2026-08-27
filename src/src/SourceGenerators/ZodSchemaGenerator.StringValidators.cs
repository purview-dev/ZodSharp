using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateStringValidations(
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		StringLengthValidators(outputContext, property, propertyName, attributes, diagnostics);

		var emailAttribute = EmailAddressAttributeData.FromAttributeData(attributes);
		if (emailAttribute.Exists)
		{
			var errorMessage =
				$"global::System.String.Format(global::System.Globalization.CultureInfo.CurrentCulture, {CodeGenHelpers.Quote(emailAttribute.ValidationAttributeData.ErrorMessage ?? "Field '{0}' must be a valid email address")}, {CodeGenHelpers.Quote(GetDisplayName(property))})";

			using (
				outputContext.Writer.OpenBlockScope(
					$"if (!global::ZodSharp.Rules.EmailRule.EmailRegex.IsMatch(value.{propertyName}))"
				)
			)
			{
				WriteValidationError(
					outputContext,
					"invalid_string",
					errorMessage,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}

			outputContext.Writer.WriteLine();
		}

		var regularExpressionAttribute = RegularExpressionAttributeData.FromAttributeData(
			attributes,
			out var regularExpressionAttributeData
		);
		if (regularExpressionAttribute.Exists)
		{
			var displayName = GetDisplayName(property);
			var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
			var messageExpression = BuildMessageExpression(
				outputContext,
				diagnostics,
				regularExpressionAttributeData,
				displayName,
				regularExpressionAttribute.ValidationAttribute,
				CodeGenHelpers.Quote(
					$"Field '{displayName}' must match the regular expression '{regularExpressionAttribute.Pattern}'."
				),
				CodeGenHelpers.Quote(displayName),
				CodeGenHelpers.Quote(regularExpressionAttribute.Pattern ?? string.Empty)
			);

			using (outputContext.Writer.OpenBlockScope())
			{
				outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				using (
					outputContext.Writer.OpenBlockScope(
						$"if ({propertyValueName}.Length != 0 && !{GetRegexFieldName(propertyName)}.IsMatch({propertyValueName}))"
					)
				)
				{
					WriteValidationError(
						outputContext,
						"invalid_string",
						messageExpression,
						CodeGenHelpers.GetPathFieldName(propertyName),
						"string"
					);
				}
			}

			outputContext.Writer.WriteLine();
		}

		GenerateUrlValidation(outputContext, property, propertyName, attributes);
		GeneratePhoneValidation(outputContext, property, propertyName, attributes);
		GenerateCreditCardValidation(outputContext, property, propertyName, attributes);
		GenerateBase64StringValidation(outputContext, property, propertyName, attributes);
	}

	static void GenerateUrlValidation(
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes
	)
	{
		var urlAttribute = UrlAttribute.FromAttributeData(attributes);
		if (!urlAttribute.Exists)
			return;

		var displayName = GetDisplayName(property);
		var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
		var messageExpression = BuildMessageExpression(
			outputContext,
			[],
			null,
			displayName,
			urlAttribute.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' must be a valid URL.")
		);

		using (outputContext.Writer.OpenBlockScope())
		{
			outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				outputContext.Writer.OpenBlockScope(
					$"if ({propertyValueName}.Length != 0 && !new global::ZodSharp.Rules.UrlRule().IsValid({propertyValueName}))"
				)
			)
			{
				WriteValidationError(
					outputContext,
					"invalid_string",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}
		}

		outputContext.Writer.WriteLine();
	}

	static void GeneratePhoneValidation(
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes
	)
	{
		var phoneAttribute = PhoneAttribute.FromAttributeData(attributes);
		if (!phoneAttribute.Exists)
			return;

		var displayName = GetDisplayName(property);
		var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
		var messageExpression = BuildMessageExpression(
			outputContext,
			[],
			null,
			displayName,
			phoneAttribute.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' must be a valid phone number.")
		);

		using (outputContext.Writer.OpenBlockScope())
		{
			outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				outputContext.Writer.OpenBlockScope(
					$"if ({propertyValueName}.Length != 0 && !new global::ZodSharp.Rules.PhoneRule().IsValid({propertyValueName}))"
				)
			)
			{
				WriteValidationError(
					outputContext,
					"invalid_string",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}
		}

		outputContext.Writer.WriteLine();
	}

	static void GenerateCreditCardValidation(
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes
	)
	{
		var creditCardAttribute = CreditCardAttributeData.FromAttributeData(attributes);
		if (!creditCardAttribute.Exists)
			return;

		var displayName = GetDisplayName(property);
		var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
		var messageExpression = BuildMessageExpression(
			outputContext,
			[],
			null,
			displayName,
			creditCardAttribute.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' must be a valid credit card number.")
		);

		using (outputContext.Writer.OpenBlockScope())
		{
			outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				outputContext.Writer.OpenBlockScope(
					$"if ({propertyValueName}.Length != 0 && !new global::ZodSharp.Rules.CreditCardRule().IsValid({propertyValueName}))"
				)
			)
			{
				WriteValidationError(
					outputContext,
					"invalid_string",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}
		}

		outputContext.Writer.WriteLine();
	}

	static void GenerateBase64StringValidation(
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes
	)
	{
		var base64StringAttribute = Base64StringAttributeData.FromAttributeData(attributes);
		if (!base64StringAttribute.Exists)
			return;

		var displayName = GetDisplayName(property);
		var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
		var messageExpression = BuildMessageExpression(
			outputContext,
			[],
			null,
			displayName,
			base64StringAttribute.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' must be a valid Base64 string.")
		);

		using (outputContext.Writer.OpenBlockScope())
		{
			outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				outputContext.Writer.OpenBlockScope(
					$"if ({propertyValueName}.Length != 0 && !new global::ZodSharp.Rules.Base64StringRule().IsValid({propertyValueName}))"
				)
			)
			{
				WriteValidationError(
					outputContext,
					"invalid_string",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}
		}

		outputContext.Writer.WriteLine();
	}

	static void StringLengthValidators(
		SchemaGenerationOutputContext outputContext,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		var displayName = GetDisplayName(property);
		var propertyPath = CodeGenHelpers.GetPathFieldName(propertyName);
		var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
		var propertyLengthName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Length");
		var lengthAttr = LengthAttributeData.FromAttributeData(attributes, out var lengthAttributeData);
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
				using (outputContext.Writer.OpenBlockScope())
				{
					var tooSmallMessage = BuildMessageExpression(
						outputContext,
						diagnostics,
						lengthAttributeData,
						displayName,
						lengthAttr.ValidationAttribute,
						$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain at least ")} + FormatCount({lengthAttr.MinimumLength}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
						CodeGenHelpers.Quote(displayName),
						lengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
						lengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
					);
					var tooBigMessage = BuildMessageExpression(
						outputContext,
						diagnostics,
						lengthAttributeData,
						displayName,
						lengthAttr.ValidationAttribute,
						$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({lengthAttr.MaximumLength}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
						CodeGenHelpers.Quote(displayName),
						lengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
						lengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
					);

					outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
					using (outputContext.Writer.OpenBlockScope($"if ({propertyValueName} is not null)"))
					{
						outputContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
						using (
							outputContext.Writer.OpenBlockScope(
								$"if ({propertyLengthName} < {lengthAttr.MinimumLength})"
							)
						)
						{
							WriteValidationError(
								outputContext,
								"too_small",
								tooSmallMessage,
								propertyPath,
								"string",
								minimum: lengthAttr.MinimumLength
							);
						}

						using (
							outputContext.Writer.OpenBlockScope(
								$"else if ({propertyLengthName} > {lengthAttr.MaximumLength})"
							)
						)
						{
							WriteValidationError(
								outputContext,
								"too_big",
								tooBigMessage,
								propertyPath,
								"string",
								maximum: lengthAttr.MaximumLength
							);
						}
					}
				}

				outputContext.Writer.WriteLine();
			}
		}

		var stringLengthAttr = StringLengthAttribute.FromAttributeData(attributes, out var stringLengthAttributeData);
		if (stringLengthAttr.Exists)
		{
			using (outputContext.Writer.OpenBlockScope())
			{
				var tooSmallMessage = BuildMessageExpression(
					outputContext,
					diagnostics,
					stringLengthAttributeData,
					displayName,
					stringLengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain at least ")} + FormatCount({stringLengthAttr.MinimumLength}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					stringLengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
					stringLengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
				);
				var tooBigMessage = BuildMessageExpression(
					outputContext,
					diagnostics,
					stringLengthAttributeData,
					displayName,
					stringLengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({stringLengthAttr.MaximumLength}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					stringLengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
					stringLengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
				);

				outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				outputContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				if (stringLengthAttr.MinimumLength > 0)
				{
					using (
						outputContext.Writer.OpenBlockScope(
							$"if ({propertyLengthName} < {stringLengthAttr.MinimumLength})"
						)
					)
					{
						WriteValidationError(
							outputContext,
							"too_small",
							tooSmallMessage,
							propertyPath,
							"string",
							minimum: stringLengthAttr.MinimumLength
						);
					}
				}

				using (
					outputContext.Writer.OpenBlockScope($"if ({propertyLengthName} > {stringLengthAttr.MaximumLength})")
				)
				{
					WriteValidationError(
						outputContext,
						"too_big",
						tooBigMessage,
						propertyPath,
						"string",
						maximum: stringLengthAttr.MaximumLength
					);
				}
			}

			outputContext.Writer.WriteLine();
		}

		var minLengthAttr = MinLengthAttributeData.FromAttributeData(attributes, out var minLengthAttributeData);
		if (minLengthAttr.Exists && minLengthAttr.Length > 0)
		{
			using (outputContext.Writer.OpenBlockScope())
			{
				var messageExpression = BuildMessageExpression(
					outputContext,
					diagnostics,
					minLengthAttributeData,
					displayName,
					minLengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain at least ")} + FormatCount({minLengthAttr.Length}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					minLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				outputContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				using (outputContext.Writer.OpenBlockScope($"if ({propertyLengthName} < {minLengthAttr.Length})"))
				{
					WriteValidationError(
						outputContext,
						"too_small",
						messageExpression,
						propertyPath,
						"string",
						minimum: minLengthAttr.Length
					);
				}
			}

			outputContext.Writer.WriteLine();
		}

		var maxLengthAttr = MaxLengthAttributeData.FromAttributeData(attributes, out var maxLengthAttributeData);
		if (maxLengthAttr.Exists && maxLengthAttr.Length >= 0)
		{
			using (outputContext.Writer.OpenBlockScope())
			{
				var messageExpression = BuildMessageExpression(
					outputContext,
					diagnostics,
					maxLengthAttributeData,
					displayName,
					maxLengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({maxLengthAttr.Length}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					maxLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				outputContext.Writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
				outputContext.Writer.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				using (outputContext.Writer.OpenBlockScope($"if ({propertyLengthName} > {maxLengthAttr.Length})"))
				{
					WriteValidationError(
						outputContext,
						"too_big",
						messageExpression,
						propertyPath,
						"string",
						maximum: maxLengthAttr.Length
					);
				}
			}

			outputContext.Writer.WriteLine();
		}
	}
}
