using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct MinLengthAttributeData(bool Exists, int Length, string? ErrorMessage)
{
	public static readonly MinLengthAttributeData Empty = new(false, 0, null);

	public static MinLengthAttributeData FromAttributeData(
		ExecutionContext executionContext,
		ImmutableArray<AttributeData> attributes
	)
	{
		if (executionContext.MinLengthAttribute is null)
			return Empty;

		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(executionContext, attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static MinLengthAttributeData FromAttributeData(
		ExecutionContext executionContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = executionContext.MinLengthAttribute;
		if (
			attributeSymbol is null
			|| !SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol)
		)
		{
			return Empty;
		}

		var length = 0;
		string? errorMessage = null;

		if (attributeData.ConstructorArguments.Length == 1 && attributeData.ConstructorArguments[0].Value is int value)
		{
			length = value;
		}

		foreach (var namedArgument in attributeData.NamedArguments)
		{
			if (namedArgument.Key == nameof(ErrorMessage) && namedArgument.Value.Value is string message)
			{
				errorMessage = message;
			}
		}

		return new(Exists: true, Length: length, ErrorMessage: errorMessage);
	}
}
