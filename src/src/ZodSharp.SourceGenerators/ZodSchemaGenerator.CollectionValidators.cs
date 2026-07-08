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
	static void GenerateCollectionValidations(
		ExecutionContext executionContext,
		StringBuilder sb,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes
	)
	{
		var isArray = propertyType is IArrayTypeSymbol;
		var minLengthAttr = MinLengthAttributeData.FromAttributeData(executionContext, attributes);
		if (minLengthAttr.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				(minLengthAttr.ErrorMessage ?? "Field '{0}' must have at least {1} items"),
				propertyName,
				minLengthAttr.Length
			);

			if (isArray)
			{
				sb.AppendLine($"            if (value.{propertyName}.Length < {minLengthAttr.Length})");
			}
			else
			{
				sb.AppendLine(
					$"            if (global::System.Linq.Enumerable.Count(value.{propertyName}) < {minLengthAttr.Length})"
				);
			}

			sb.AppendLine("            {");
			sb.AppendLine($"                errors.Add(new {TypeHelpers.ValidationError.Global()}(");
			sb.AppendLine("                    \"too_small\",");
			sb.AppendLine($"                    \"{errorMessage}\",");
			sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
			sb.AppendLine("                ));");
			sb.AppendLine("            }");
		}

		var maxLengthAttr = MaxLengthAttributeData.FromAttributeData(executionContext, attributes);
		if (maxLengthAttr.Exists)
		{
			{
				var errorMessage = string.Format(
					CultureInfo.InvariantCulture,
					(maxLengthAttr.ErrorMessage ?? "Field '{0}' must have at most {1} items"),
					propertyName,
					maxLengthAttr.Length
				);

				if (isArray)
				{
					sb.AppendLine($"            if (value.{propertyName}.Length > {maxLengthAttr.Length})");
				}
				else
				{
					sb.AppendLine(
						$"            if (global::System.Linq.Enumerable.Count(value.{propertyName}) > {maxLengthAttr.Length})"
					);
				}

				sb.AppendLine("            {");
				sb.AppendLine($"                errors.Add(new {TypeHelpers.ValidationError.Global()}(");
				sb.AppendLine("                    \"too_big\",");
				sb.AppendLine($"                    \"{errorMessage}\",");
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}
		}
	}
}
