using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct RegularExpressionAttributeData(
	bool Exists,
	string? Pattern,
	string? ErrorMessage,
	string? ErrorMessageResourceName,
	INamedTypeSymbol? ErrorMessageResourceType
)
{
	public static readonly RegularExpressionAttributeData Empty = new(false, null, null, null, null);

	public static RegularExpressionAttributeData FromAttributeData(
		GenerationContext generationContext,
		ImmutableArray<AttributeData> attributeData
	)
	{
		if (generationContext is null)
		{
			throw new ArgumentNullException(nameof(generationContext));
		}

		if (generationContext.RegularExpressionAttribute is not null)
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

	public static RegularExpressionAttributeData FromAttributeData(
		GenerationContext generationContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = generationContext.RegularExpressionAttribute;
		var exists =
			attributeSymbol is not null
			&& SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol);
		string? pattern = null;
		var errorMessage = (string?)null;
		string? errorMessageResourceName = null;
		INamedTypeSymbol? errorMessageResourceType = null;
		if (exists)
		{
			var constructorArguments = attributeData.ConstructorArguments;
			if (constructorArguments.Length > 0)
			{
				var patternArgument = constructorArguments[0];
				if (patternArgument.Value is string value)
					pattern = value;
			}

			foreach (var namedArg in attributeData.NamedArguments)
			{
				switch (namedArg.Key)
				{
					case nameof(ErrorMessage) when namedArg.Value.Value is string errorMsg:
						errorMessage = errorMsg;
						break;
					case nameof(ErrorMessageResourceName) when namedArg.Value.Value is string resourceName:
						errorMessageResourceName = resourceName;
						break;
					case nameof(ErrorMessageResourceType) when namedArg.Value.Value is INamedTypeSymbol resourceType:
						errorMessageResourceType = resourceType;
						break;
				}
			}
		}

		return new(exists, pattern, errorMessage, errorMessageResourceName, errorMessageResourceType);
	}
}
