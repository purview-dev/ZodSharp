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
		if (rangeAttr.Exists)
		{
			// This will never account for other numeric kinds, or OperandTypes of Double or Int...!
			if (rangeAttr.Kind is RangeAttributeKind.Int32 or RangeAttributeKind.Double)
			{
				var minExclusive = rangeAttr.MinimumIsExclusive ? "exclusive" : "inclusive";
				var maxExclusive = rangeAttr.MaximumIsExclusive ? "exclusive" : "inclusive";

				var errorMessage = string.Format(
					CultureInfo.InvariantCulture,
					(rangeAttr.ErrorMessage ?? "Field '{0}' must be between {1} ({2}) and {3} ({4})"),
					propertyName,
					rangeAttr.Minimum,
					minExclusive,
					rangeAttr.Maximum,
					maxExclusive
				);

				var minComparison = rangeAttr.MinimumIsExclusive ? "<" : "<=";
				var maxComparison = rangeAttr.MaximumIsExclusive ? ">" : ">=";

				sb.AppendLine(
					$"            if (value.{propertyName} {minComparison} {rangeAttr.Minimum} || value.{propertyName} {maxComparison} {rangeAttr.Maximum})"
				);
				sb.AppendLine("            {");
				sb.AppendLine($"                errors.Add(new {TypeHelpers.ValidationError.Global()}(");
				sb.AppendLine("                    \"invalid_number\",");
				sb.AppendLine($"                    \"{errorMessage}\",");
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}
		}
	}
}
