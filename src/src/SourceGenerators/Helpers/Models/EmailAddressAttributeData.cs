using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct EmailAddressAttributeData(bool Exists, string? ErrorMessage)
{
	public static readonly EmailAddressAttributeData Empty = new(false, null);

	public static EmailAddressAttributeData FromAttributeData(
		ExecutionContext executionContext,
		ImmutableArray<AttributeData> attributeData
	)
	{
		if (executionContext.EmailAddressAttribute is not null)
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

	public static EmailAddressAttributeData FromAttributeData(
		ExecutionContext executionContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = executionContext.EmailAddressAttribute;
		var exists =
			attributeSymbol is not null
			&& SymbolEqualityComparer.Default.Equals(attributeData?.AttributeClass, attributeSymbol);
		var errorMessage = (string?)null;
		if (exists)
		{
			foreach (var namedArg in attributeData!.NamedArguments)
			{
				if (namedArg.Key == nameof(ErrorMessage) && namedArg.Value.Value is string errorMsg)
					errorMessage = errorMsg;
			}
		}

		return new(exists, errorMessage);
	}
}
