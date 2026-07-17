using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct DeniedValuesAttributeData(
	bool Exists,
	ImmutableArray<TypedConstant> Values,
	string? ErrorMessage,
	string? ErrorMessageResourceName,
	INamedTypeSymbol? ErrorMessageResourceType
)
{
	public static readonly DeniedValuesAttributeData Empty = new(false, [], null, null, null);

	public static DeniedValuesAttributeData FromAttributeData(
		GenerationContext generationContext,
		ImmutableArray<AttributeData> attributes
	)
	{
		if (generationContext.DeniedValuesAttribute is null)
			return Empty;

		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(generationContext, attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static DeniedValuesAttributeData FromAttributeData(
		GenerationContext generationContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = generationContext.DeniedValuesAttribute;
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
		var values = attributeData.ConstructorArguments[0].Values;

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
			Values: values,
			ErrorMessage: errorMessage,
			ErrorMessageResourceName: errorMessageResourceName,
			ErrorMessageResourceType: errorMessageResourceType
		);
	}
}
