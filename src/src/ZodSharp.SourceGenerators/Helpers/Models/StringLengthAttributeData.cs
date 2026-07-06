using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct StringLengthAttribute(bool Exists, int MaximumLength, int MinimumLength, string? ErrorMessage)
{
	public static readonly StringLengthAttribute Empty = new(false, int.MaxValue, 0, null);

	public static StringLengthAttribute FromAttributeData(
		ExecutionContext executionContext,
		ImmutableArray<AttributeData> attributes
	)
	{
		if (executionContext.StringLengthAttribute is null)
			return Empty;

		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(executionContext, attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static StringLengthAttribute FromAttributeData(
		ExecutionContext executionContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = executionContext.StringLengthAttribute;

		if (
			attributeSymbol is null
			|| !SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol)
		)
		{
			return Empty;
		}

		var maximumLength = int.MaxValue;
		var minimumLength = 0;
		string? errorMessage = null;

		if (attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is int maximum)
		{
			maximumLength = maximum;
		}

		foreach (var namedArgument in attributeData.NamedArguments)
		{
			switch (namedArgument.Key)
			{
				case nameof(MinimumLength) when namedArgument.Value.Value is int minimum:
					minimumLength = minimum;
					break;

				case nameof(ErrorMessage) when namedArgument.Value.Value is string message:
					errorMessage = message;
					break;
			}
		}

		return new(
			Exists: true,
			MaximumLength: maximumLength,
			MinimumLength: minimumLength,
			ErrorMessage: errorMessage
		);
	}
}
