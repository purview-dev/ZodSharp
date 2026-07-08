using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Helpers.Models;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateStringValidations(
		ExecutionContext executionContext,
		string propertyName,
		ImmutableArray<AttributeData> attributes
	)
	{
		var stringLengthAttr = StringLengthAttribute.FromAttributeData(executionContext, attributes);
		if (stringLengthAttr.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				stringLengthAttr.ErrorMessage ?? "Field '{0}' must be between {1} and {2} characters long",
				propertyName,
				stringLengthAttr.MaximumLength,
				stringLengthAttr.MinimumLength
			);

			if (stringLengthAttr.MinimumLength > 0)
			{
				executionContext.Writer.WriteRule(
					propertyName,
					$"value.{propertyName}.Length < {stringLengthAttr.MinimumLength}",
					"too_small",
					errorMessage
				);
			}

			executionContext.Writer.WriteRule(
				propertyName,
				$"value.{propertyName}.Length > {stringLengthAttr.MaximumLength}",
				"too_big",
				errorMessage
			);
		}

		var minLengthAttr = MinLengthAttributeData.FromAttributeData(executionContext, attributes);
		if (minLengthAttr.Exists)
		{
			if (minLengthAttr.Length > 0)
			{
				var errorMessage = string.Format(
					CultureInfo.InvariantCulture,
					minLengthAttr.ErrorMessage ?? "Field '{0}' must be at least {1} characters long",
					propertyName,
					minLengthAttr.Length
				);

				executionContext.Writer.WriteRule(
					propertyName,
					$"value.{propertyName}.Length < {minLengthAttr.Length}",
					"too_small",
					errorMessage
				);
			}
		}

		var maxLengthAttr = MaxLengthAttributeData.FromAttributeData(executionContext, attributes);
		if (maxLengthAttr.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				maxLengthAttr.ErrorMessage ?? "Field '{0}' must be at most {1} characters long",
				propertyName,
				maxLengthAttr.Length
			);

			executionContext.Writer.WriteRule(
				propertyName,
				$"value.{propertyName}.Length > {maxLengthAttr.Length}",
				"too_big",
				errorMessage
			);
		}

		var emailAttribute = EmailAddressAttributeData.FromAttributeData(executionContext, attributes);
		if (emailAttribute.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				(emailAttribute.ErrorMessage ?? "Field '{0}' must be a valid email address"),
				propertyName
			);

			executionContext.Writer.WriteRule(
				propertyName,
				$"!global::ZodSharp.Rules.EmailRule.EmailRegex.IsMatch(value.{propertyName})",
				"invalid_string",
				errorMessage
			);
		}
	}
}
