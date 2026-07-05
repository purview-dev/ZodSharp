using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct RequiredAttributeData(bool Exists, bool AllowEmptyStrings, string? ErrorMessage)
{
	public static readonly RequiredAttributeData Empty = new(false, false, null);

	public static RequiredAttributeData FromAttributeData(
		ExecutionContext executionContext,
		ImmutableArray<AttributeData> attributeData
	)
	{
		if (executionContext.RequiredAttribute is not null)
		{
			for (var i = 0; i < attributeData.Length; i++)
			{
				var result = FromAttributeData(executionContext, attributeData[i]);
				if (result.Exists)
					return result;
			}
		}

		return Empty;
	}

	public static RequiredAttributeData FromAttributeData(
		ExecutionContext executionContext,
		AttributeData attributeData
	)
	{
		var exists =
			executionContext.RequiredAttribute is not null
			&& SymbolEqualityComparer.Default.Equals(attributeData?.AttributeClass, executionContext.RequiredAttribute);
		var allowEmptyStrings = false;
		var errorMessage = (string?)null;
		if (exists)
		{
			foreach (var namedArg in attributeData!.NamedArguments)
			{
				if (namedArg.Key == nameof(AllowEmptyStrings) && namedArg.Value.Value is bool allowEmpty)
					allowEmptyStrings = allowEmpty;
				else if (namedArg.Key == nameof(ErrorMessage) && namedArg.Value.Value is string errorMsg)
					errorMessage = errorMsg;
			}
		}

		return new(exists, allowEmptyStrings, errorMessage);
	}
}
