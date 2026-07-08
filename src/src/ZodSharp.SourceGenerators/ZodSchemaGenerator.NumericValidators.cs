using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Helpers.Models;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateNumericValidations(
		ExecutionContext executionContext,
		string propertyName,
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

				var minComparison = rangeAttr.MinimumIsExclusive ? "<=" : "<";
				var maxComparison = rangeAttr.MaximumIsExclusive ? ">=" : ">";

				executionContext.Writer.WriteRule(
					propertyName,
					$"value.{propertyName} {minComparison} {rangeAttr.Minimum} || value.{propertyName} {maxComparison} {rangeAttr.Maximum}",
					"invalid_number",
					errorMessage
				);
			}
		}
	}
}
