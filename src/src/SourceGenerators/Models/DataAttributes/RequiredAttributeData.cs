using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct RequiredAttributeData(bool Exists, bool AllowEmptyStrings, string? ErrorMessage)
{
	public static readonly RequiredAttributeData Empty = new(false, false, null);

	public static RequiredAttributeData FromAttributeData(
		GenerationContext generationContext,
		ImmutableArray<AttributeData> attributeData
	)
	{
		if (generationContext.RequiredAttribute is not null)
		{
			for (var i = 0; i < attributeData.Length; i++)
			{
				var result = FromAttributeData(generationContext, attributeData[i]);
				if (result.Exists)
					return result;
			}
		}

		return Empty;
	}

	public static RequiredAttributeData FromAttributeData(
		GenerationContext generationContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = generationContext.RequiredAttribute;
		var exists =
			attributeSymbol is not null
			&& SymbolEqualityComparer.Default.Equals(attributeData?.AttributeClass, attributeSymbol);
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
