using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Helpers.Models;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateCollectionValidations(
		ExecutionContext executionContext,
		ITypeSymbol propertyType,
		string propertyName,
		ImmutableArray<AttributeData> attributes
	)
	{
		var isArray = propertyType is IArrayTypeSymbol;

		var lengthAttr = LengthAttributeData.FromAttributeData(executionContext, attributes);

		if (lengthAttr.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				lengthAttr.ErrorMessage ?? "Field '{0}' must contain between {1} and {2} element(s).",
				propertyName,
				lengthAttr.MinimumLength,
				lengthAttr.MaximumLength
			);

			var countExpression = isArray
				? $"value.{propertyName}.Length"
				: $"global::System.Linq.Enumerable.Count(value.{propertyName})";

			executionContext.Writer.WriteRule(
				propertyName,
				$"{countExpression} < {lengthAttr.MinimumLength}",
				"too_small",
				errorMessage
			);

			executionContext.Writer.WriteRule(
				propertyName,
				$"{countExpression} > {lengthAttr.MaximumLength}",
				"too_big",
				errorMessage
			);
		}

		var minLengthAttr = MinLengthAttributeData.FromAttributeData(executionContext, attributes);
		if (minLengthAttr.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				(minLengthAttr.ErrorMessage ?? "Field '{0}' must have at least {1} items"),
				propertyName,
				minLengthAttr.Length
			);

			var comparison = isArray
				? $"value.{propertyName}.Length < {minLengthAttr.Length}"
				: $"if (global::System.Linq.Enumerable.Count(value.{propertyName}) < {minLengthAttr.Length})";

			executionContext.Writer.WriteRule(propertyName, comparison, "too_small", errorMessage);
		}

		var maxLengthAttr = MaxLengthAttributeData.FromAttributeData(executionContext, attributes);
		if (maxLengthAttr.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				(maxLengthAttr.ErrorMessage ?? "Field '{0}' must have at most {1} items"),
				propertyName,
				maxLengthAttr.Length
			);

			var comparison = isArray
				? $"value.{propertyName}.Length > {maxLengthAttr.Length}"
				: $"if (global::System.Linq.Enumerable.Count(value.{propertyName}) > {maxLengthAttr.Length})";

			executionContext.Writer.WriteRule(propertyName, comparison, "too_big", errorMessage);
		}
	}
}
