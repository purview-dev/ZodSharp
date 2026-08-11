using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct RequiredAttributeData(
	bool Exists,
	bool AllowEmptyStrings,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly RequiredAttributeData Empty = new(
		false,
		false,
		ValidationAttributeData.Empty
	);

	public static RequiredAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributeData
	)
	{
		for (var i = 0; i < attributeData.Length; i++)
		{
			var result = FromAttributeData(attributeData[i]);
			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static RequiredAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.RequiredAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		attributeData.TryGetNamedArgument<bool>(
			nameof(AllowEmptyStrings),
			out var allowEmptyStrings
		);
		var validationAttribute = ValidationAttributeData.FromAttributeData(attributeData);

		return new(true, allowEmptyStrings, validationAttribute);
	}
}
