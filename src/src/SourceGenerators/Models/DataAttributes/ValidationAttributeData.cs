using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct ValidationAttributeData(
	bool Exists,
	string? ErrorMessage,
	string? ErrorMessageResourceName,
	INamedTypeSymbol? ErrorMessageResourceType
)
{
	public static readonly ValidationAttributeData Empty = new(false, null, null, null);

	public static ValidationAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributes
	)
	{
		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(attributes[i]);
			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static ValidationAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (
			attributeData.AttributeClass is null
			|| !TypeHelpers.InheritsFrom(
				attributeData.AttributeClass,
				TypeLibrary.DataAnnotations.ValidationAttribute
			)
		)
			return Empty;

		attributeData.TryGetNamedArgument<string>(nameof(ErrorMessage), out var errorMessage);
		attributeData.TryGetNamedArgument<string>(
			nameof(ErrorMessageResourceName),
			out var errorMessageResourceName
		);
		attributeData.TryGetNamedArgument<INamedTypeSymbol>(
			nameof(ErrorMessageResourceType),
			out var errorMessageResourceType
		);

		return new(
			Exists: true,
			ErrorMessage: errorMessage,
			ErrorMessageResourceName: errorMessageResourceName,
			ErrorMessageResourceType: errorMessageResourceType
		);
	}
}
