using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers.Models;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	/// <summary>
	/// Generates numeric-specific validations.
	/// </summary>
	static void GenerateNumericValidations(
		ExecutionContext executionContext,
		StringBuilder sb,
		string propertyName,
		ITypeSymbol _, //propertyType,
		ImmutableArray<AttributeData> attributes
	)
	{
		var rangeAttr = RangeAttributeData.FromAttributeData(executionContext, attributes);
		if (!rangeAttr.Exists)
		{
			// This will never account for other numeric kinds, or OperandTypes of Double or Int...!
			if (rangeAttr.Kind is RangeAttributeKind.Double or RangeAttributeKind.Double)
			{
				var errorMessage = string.Format(
					CultureInfo.InvariantCulture,
					(rangeAttr.ErrorMessage ?? $"Field '{0}' must be between {1} and {2}"),
					propertyName,
					rangeAttr.Minimum,
					rangeAttr.Maximum
				);
				;

				sb.AppendLine(
					$"            if (value.{propertyName} < {rangeAttr.Minimum} || value.{propertyName} > {rangeAttr.Maximum})"
				);
				sb.AppendLine("            {");
				sb.AppendLine("                errors.Add(new ValidationError(");
				sb.AppendLine("                    \"invalid_number\",");
				sb.AppendLine($"                    \"{errorMessage}\",");
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}
		}
	}
}
