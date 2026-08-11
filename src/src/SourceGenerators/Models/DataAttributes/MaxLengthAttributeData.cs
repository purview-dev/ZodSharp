using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct MaxLengthAttributeData(
	bool Exists,
	int Length,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly MaxLengthAttributeData Empty = new(
		false,
		0,
		ValidationAttributeData.Empty
	);

	public static MaxLengthAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributes
	) => FromAttributeData(attributes, out _);

	public static MaxLengthAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributes,
		out AttributeData? attribute
	)
	{
		attribute = null;
		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(attributes[i]);
			if (result.Exists)
			{
				attribute = attributes[i];
				return result;
			}
		}

		return Empty;
	}

	public static MaxLengthAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.MaxLengthAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		if (!attributeData.TryGetConstructorArgument<int>("length", out var length))
			attributeData.TryGetNamedArgument(nameof(Length), out length);

		var validationAttribute = ValidationAttributeData.FromAttributeData(attributeData);

		return new(true, length, validationAttribute);
	}
}
