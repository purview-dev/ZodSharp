using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct LengthAttributeData(
	bool Exists,
	int MinimumLength,
	int MaximumLength,
	string? ErrorMessage,
	string? ErrorMessageResourceName,
	INamedTypeSymbol? ErrorMessageResourceType
)
{
	public static readonly LengthAttributeData Empty = new(false, 0, int.MaxValue, null, null, null);

	public static LengthAttributeData FromAttributeData(
		GenerationContext generationContext,
		ImmutableArray<AttributeData> attributes
	)
	{
		if (generationContext.LengthAttribute is null)
			return Empty;

		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(generationContext, attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static LengthAttributeData FromAttributeData(
		GenerationContext generationContext,
		AttributeData attributeData
	)
	{
		if (generationContext is null)
		{
			throw new ArgumentNullException(nameof(generationContext));
		}

		var attributeSymbol = generationContext.LengthAttribute;

		if (
			attributeSymbol is null
			|| !SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol)
		)
		{
			return Empty;
		}

		string? errorMessage = null;
		string? errorMessageResourceName = null;
		INamedTypeSymbol? errorMessageResourceType = null;

		var minimumLength = (int)attributeData.ConstructorArguments[0].Value!;
		var maximumLength = (int)attributeData.ConstructorArguments[1].Value!;

		foreach (var namedArgument in attributeData.NamedArguments)
		{
			switch (namedArgument.Key)
			{
				case nameof(ErrorMessage) when namedArgument.Value.Value is string message:
					errorMessage = message;
					break;

				case nameof(ErrorMessageResourceName) when namedArgument.Value.Value is string resourceName:
					errorMessageResourceName = resourceName;
					break;

				case nameof(ErrorMessageResourceType) when namedArgument.Value.Value is INamedTypeSymbol resourceType:
					errorMessageResourceType = resourceType;
					break;
			}
		}

		return new(
			Exists: true,
			MinimumLength: minimumLength,
			MaximumLength: maximumLength,
			ErrorMessage: errorMessage,
			ErrorMessageResourceName: errorMessageResourceName,
			ErrorMessageResourceType: errorMessageResourceType
		);
	}
}
