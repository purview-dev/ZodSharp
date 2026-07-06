using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Helpers.Models;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateStringValidations(
		ExecutionContext executionContext,
		StringBuilder sb,
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
				sb.AppendLine($"           if (value.{propertyName}.Length < {stringLengthAttr.MinimumLength})");
				sb.AppendLine("            {");
				sb.AppendLine($"                errors.Add(new {TypeHelpers.ValidationError.Global()}(");
				sb.AppendLine("                    \"too_small\",");
				sb.AppendLine($"                    \"{errorMessage}\",");
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}

			sb.AppendLine($"           if (value.{propertyName}.Length > {stringLengthAttr.MaximumLength})");
			sb.AppendLine("            {");
			sb.AppendLine($"                errors.Add(new {TypeHelpers.ValidationError.Global()}(");
			sb.AppendLine("                    \"too_big\",");
			sb.AppendLine($"                    \"{errorMessage}\",");
			sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
			sb.AppendLine("                ));");
			sb.AppendLine("            }");
		}

		var minLengthAttr = MinLengthAttributeData.FromAttributeData(executionContext, attributes);
		if (minLengthAttr.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				minLengthAttr.ErrorMessage ?? "Field '{0}' must be at least {1} characters long",
				propertyName,
				minLengthAttr.Length
			);

			if (minLengthAttr.Length > 0)
			{
				sb.AppendLine($"           if (value.{propertyName}.Length < {minLengthAttr.Length})");
				sb.AppendLine("            {");
				sb.AppendLine($"                errors.Add(new {TypeHelpers.ValidationError.Global()}(");
				sb.AppendLine("                    \"too_small\",");
				sb.AppendLine($"                    \"{errorMessage}\",");
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
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

			sb.AppendLine($"           if (value.{propertyName}.Length > {maxLengthAttr.Length})");
			sb.AppendLine("            {");
			sb.AppendLine($"                errors.Add(new {TypeHelpers.ValidationError.Global()}(");
			sb.AppendLine("                    \"too_big\",");
			sb.AppendLine($"                    \"{errorMessage}\",");
			sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
			sb.AppendLine("                ));");
			sb.AppendLine("            }");
		}

		var emailAttribute = EmailAddressAttributeData.FromAttributeData(executionContext, attributes);
		if (emailAttribute.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				(emailAttribute.ErrorMessage ?? "Field '{0}' must be a valid email address"),
				propertyName
			);

			sb.AppendLine(
				$"            if (!global::ZodSharp.Rules.EmailRule.EmailRegex.IsMatch(value.{propertyName}))"
			);
			sb.AppendLine("            {");
			sb.AppendLine($"                errors.Add(new {TypeHelpers.ValidationError.Global()}(");
			sb.AppendLine("                    \"invalid_string\",");
			sb.AppendLine($"                    \"{errorMessage}\",");
			sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
			sb.AppendLine("                ));");
			sb.AppendLine("            }");
		}
	}
}
