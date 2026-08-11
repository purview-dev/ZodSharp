using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct MinLengthAttributeData(
	bool Exists,
	int Length,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly MinLengthAttributeData Empty = new(
		false,
		0,
		ValidationAttributeData.Empty
	);

	public static MinLengthAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributes
	) => FromAttributeData(attributes, out _);

	public static MinLengthAttributeData FromAttributeData(
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

	public static MinLengthAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.MinLengthAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		if (!attributeData.TryGetConstructorArgument<int>("length", out var length))
			attributeData.TryGetNamedArgument(nameof(Length), out length);

		var validationAttribute = ValidationAttributeData.FromAttributeData(attributeData);

		return new(true, length, validationAttribute);
	}
}
