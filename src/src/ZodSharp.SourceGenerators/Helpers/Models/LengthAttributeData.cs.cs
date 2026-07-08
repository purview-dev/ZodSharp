using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct LengthAttributeData(bool Exists, int MinimumLength, int MaximumLength, string? ErrorMessage)
{
	public static readonly LengthAttributeData Empty = new(false, 0, int.MaxValue, null);

	public static LengthAttributeData FromAttributeData(
		ExecutionContext executionContext,
		ImmutableArray<AttributeData> attributes
	)
	{
		if (executionContext.LengthAttribute is null)
			return Empty;

		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(executionContext, attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static LengthAttributeData FromAttributeData(ExecutionContext executionContext, AttributeData attributeData)
	{
		var attributeSymbol = executionContext.LengthAttribute;

		if (
			attributeSymbol is null
			|| !SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol)
		)
		{
			return Empty;
		}

		string? errorMessage = null;

		var minimumLength = (int)attributeData.ConstructorArguments[0].Value!;
		var maximumLength = (int)attributeData.ConstructorArguments[1].Value!;

		foreach (var namedArgument in attributeData.NamedArguments)
		{
			switch (namedArgument.Key)
			{
				case nameof(ErrorMessage) when namedArgument.Value.Value is string message:
					errorMessage = message;
					break;
			}
		}

		return new(
			Exists: true,
			MinimumLength: minimumLength,
			MaximumLength: maximumLength,
			ErrorMessage: errorMessage
		);
	}
}
