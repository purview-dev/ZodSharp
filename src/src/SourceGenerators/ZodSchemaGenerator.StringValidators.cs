using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateStringValidations(
		GenerationContext generationContext,
		GenerationLogger? logger,
		IPropertySymbol property,
		string propertyName,
		ImmutableArray<AttributeData> attributes,
		List<DiagnosticInfo> diagnostics
	)
	{
		StringLengthValidators(generationContext, logger, property, propertyName, attributes, diagnostics);

		var emailAttribute = EmailAddressAttributeData.FromAttributeData(attributes);
		if (emailAttribute.Exists)
		{
			var errorMessage =
				$"global::System.String.Format(global::System.Globalization.CultureInfo.CurrentCulture, {CodeGenHelpers.Quote(emailAttribute.ValidationAttributeData.ErrorMessage ?? "Field '{0}' must be a valid email address")}, {CodeGenHelpers.Quote(GetDisplayName(property))})";

			using (
				generationContext.CodeWriter.OpenBlockScope(
					$"if (!global::ZodSharp.Rules.EmailRule.EmailRegex.IsMatch(value.{propertyName}))"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_string",
					errorMessage,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}

			generationContext.CodeWriter.WriteLine();
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
				logger,
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

			using (generationContext.CodeWriter.OpenBlockScope())
			{
				generationContext.CodeWriter.WriteLine($"var {propertyValueName} = value.{propertyName};");
				using (
					generationContext.CodeWriter.OpenBlockScope(
						$"if ({propertyValueName}.Length != 0 && !{GetRegexFieldName(propertyName)}.IsMatch({propertyValueName}))"
					)
				)
				{
					WriteValidationError(
						generationContext,
						"invalid_string",
						messageExpression,
						CodeGenHelpers.GetPathFieldName(propertyName),
						"string"
					);
				}
			}

			generationContext.CodeWriter.WriteLine();
		}

		GenerateUrlValidation(generationContext, logger, property, propertyName, attributes);
		GeneratePhoneValidation(generationContext, logger, property, propertyName, attributes);
		GenerateCreditCardValidation(generationContext, logger, property, propertyName, attributes);
		GenerateBase64StringValidation(generationContext, logger, property, propertyName, attributes);
	}

	static void GenerateUrlValidation(
		GenerationContext generationContext,
		GenerationLogger? logger,
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
			logger,
			[],
			null,
			displayName,
			urlAttribute.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' must be a valid URL.")
		);

		using (generationContext.CodeWriter.OpenBlockScope())
		{
			generationContext.CodeWriter.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				generationContext.CodeWriter.OpenBlockScope(
					$"if ({propertyValueName}.Length != 0 && !new global::ZodSharp.Rules.UrlRule().IsValid({propertyValueName}))"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_string",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}
		}

		generationContext.CodeWriter.WriteLine();
	}

	static void GeneratePhoneValidation(
		GenerationContext generationContext,
		GenerationLogger? logger,
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
			logger,
			[],
			null,
			displayName,
			phoneAttribute.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' must be a valid phone number.")
		);

		using (generationContext.CodeWriter.OpenBlockScope())
		{
			generationContext.CodeWriter.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				generationContext.CodeWriter.OpenBlockScope(
					$"if ({propertyValueName}.Length != 0 && !new global::ZodSharp.Rules.PhoneRule().IsValid({propertyValueName}))"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_string",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}
		}

		generationContext.CodeWriter.WriteLine();
	}

	static void GenerateCreditCardValidation(
		GenerationContext generationContext,
		GenerationLogger? logger,
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
			logger,
			[],
			null,
			displayName,
			creditCardAttribute.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' must be a valid credit card number.")
		);

		using (generationContext.CodeWriter.OpenBlockScope())
		{
			generationContext.CodeWriter.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				generationContext.CodeWriter.OpenBlockScope(
					$"if ({propertyValueName}.Length != 0 && !new global::ZodSharp.Rules.CreditCardRule().IsValid({propertyValueName}))"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_string",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}
		}

		generationContext.CodeWriter.WriteLine();
	}

	static void GenerateBase64StringValidation(
		GenerationContext generationContext,
		GenerationLogger? logger,
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
			logger,
			[],
			null,
			displayName,
			base64StringAttribute.ValidationAttribute,
			CodeGenHelpers.Quote($"Field '{displayName}' must be a valid Base64 string.")
		);

		using (generationContext.CodeWriter.OpenBlockScope())
		{
			generationContext.CodeWriter.WriteLine($"var {propertyValueName} = value.{propertyName};");
			using (
				generationContext.CodeWriter.OpenBlockScope(
					$"if ({propertyValueName}.Length != 0 && !new global::ZodSharp.Rules.Base64StringRule().IsValid({propertyValueName}))"
				)
			)
			{
				WriteValidationError(
					generationContext,
					"invalid_string",
					messageExpression,
					CodeGenHelpers.GetPathFieldName(propertyName),
					"string"
				);
			}
		}

		generationContext.CodeWriter.WriteLine();
	}

	static void StringLengthValidators(
		GenerationContext generationContext,
		GenerationLogger? logger,
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
				using (generationContext.CodeWriter.OpenBlockScope())
				{
					var tooSmallMessage = BuildMessageExpression(
						logger,
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
						logger,
						diagnostics,
						lengthAttributeData,
						displayName,
						lengthAttr.ValidationAttribute,
						$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({lengthAttr.MaximumLength}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
						CodeGenHelpers.Quote(displayName),
						lengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
						lengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
					);

					generationContext.CodeWriter.WriteLine($"var {propertyValueName} = value.{propertyName};");
					using (generationContext.CodeWriter.OpenBlockScope($"if ({propertyValueName} is not null)"))
					{
						generationContext.CodeWriter.WriteLine(
							$"var {propertyLengthName} = {propertyValueName}.Length;"
						);
						using (
							generationContext.CodeWriter.OpenBlockScope(
								$"if ({propertyLengthName} < {lengthAttr.MinimumLength})"
							)
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
							generationContext.CodeWriter.OpenBlockScope(
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

				generationContext.CodeWriter.WriteLine();
			}
		}

		var stringLengthAttr = StringLengthAttribute.FromAttributeData(attributes, out var stringLengthAttributeData);
		if (stringLengthAttr.Exists)
		{
			using (generationContext.CodeWriter.OpenBlockScope())
			{
				var tooSmallMessage = BuildMessageExpression(
					logger,
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
					logger,
					diagnostics,
					stringLengthAttributeData,
					displayName,
					stringLengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({stringLengthAttr.MaximumLength}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					stringLengthAttr.MaximumLength.ToString(CultureInfo.InvariantCulture),
					stringLengthAttr.MinimumLength.ToString(CultureInfo.InvariantCulture)
				);

				generationContext.CodeWriter.WriteLine($"var {propertyValueName} = value.{propertyName};");
				generationContext.CodeWriter.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				if (stringLengthAttr.MinimumLength > 0)
				{
					using (
						generationContext.CodeWriter.OpenBlockScope(
							$"if ({propertyLengthName} < {stringLengthAttr.MinimumLength})"
						)
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

				using (
					generationContext.CodeWriter.OpenBlockScope(
						$"if ({propertyLengthName} > {stringLengthAttr.MaximumLength})"
					)
				)
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

			generationContext.CodeWriter.WriteLine();
		}

		var minLengthAttr = MinLengthAttributeData.FromAttributeData(attributes, out var minLengthAttributeData);
		if (minLengthAttr.Exists && minLengthAttr.Length > 0)
		{
			using (generationContext.CodeWriter.OpenBlockScope())
			{
				var messageExpression = BuildMessageExpression(
					logger,
					diagnostics,
					minLengthAttributeData,
					displayName,
					minLengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain at least ")} + FormatCount({minLengthAttr.Length}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					minLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				generationContext.CodeWriter.WriteLine($"var {propertyValueName} = value.{propertyName};");
				generationContext.CodeWriter.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				using (
					generationContext.CodeWriter.OpenBlockScope($"if ({propertyLengthName} < {minLengthAttr.Length})")
				)
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

			generationContext.CodeWriter.WriteLine();
		}

		var maxLengthAttr = MaxLengthAttributeData.FromAttributeData(attributes, out var maxLengthAttributeData);
		if (maxLengthAttr.Exists && maxLengthAttr.Length >= 0)
		{
			using (generationContext.CodeWriter.OpenBlockScope())
			{
				var messageExpression = BuildMessageExpression(
					logger,
					diagnostics,
					maxLengthAttributeData,
					displayName,
					maxLengthAttr.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({maxLengthAttr.Length}, {CodeGenHelpers.Quote("character")}, {CodeGenHelpers.Quote("characters")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					maxLengthAttr.Length.ToString(CultureInfo.InvariantCulture)
				);

				generationContext.CodeWriter.WriteLine($"var {propertyValueName} = value.{propertyName};");
				generationContext.CodeWriter.WriteLine($"var {propertyLengthName} = {propertyValueName}.Length;");
				using (
					generationContext.CodeWriter.OpenBlockScope($"if ({propertyLengthName} > {maxLengthAttr.Length})")
				)
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

			generationContext.CodeWriter.WriteLine();
		}
	}
}
