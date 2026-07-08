using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct DeniedValuesAttributeData(bool Exists, object?[] Objects, string? ErrorMessage)
{
	public static readonly DeniedValuesAttributeData Empty = new(false, [], null);

	public static DeniedValuesAttributeData FromAttributeData(
		ExecutionContext executionContext,
		ImmutableArray<AttributeData> attributes
	)
	{
		if (executionContext.DeniedValuesAttribute is null)
			return Empty;

		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(executionContext, attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static DeniedValuesAttributeData FromAttributeData(
		ExecutionContext executionContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = executionContext.DeniedValuesAttribute;
		if (
			attributeSymbol is null
			|| !SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol)
		)
		{
			return Empty;
		}

		string? errorMessage = null;

		var objects = (object?[])attributeData.ConstructorArguments[0].Value!;

		foreach (var namedArgument in attributeData.NamedArguments)
		{
			switch (namedArgument.Key)
			{
				case nameof(ErrorMessage) when namedArgument.Value.Value is string message:
					errorMessage = message;
					break;
			}
		}

		return new(Exists: true, Objects: objects, ErrorMessage: errorMessage);
	}
}
