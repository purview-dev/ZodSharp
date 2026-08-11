using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct LengthAttributeData(
	bool Exists,
	int MinimumLength,
	int MaximumLength,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly LengthAttributeData Empty = new(
		false,
		0,
		int.MaxValue,
		ValidationAttributeData.Empty
	);

	public static LengthAttributeData FromAttributeData(ImmutableArray<AttributeData> attributes) =>
		FromAttributeData(attributes, out _);

	public static LengthAttributeData FromAttributeData(
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

	public static LengthAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.LengthAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		var minimumLength = (int)attributeData.ConstructorArguments[0].Value!;
		var maximumLength = (int)attributeData.ConstructorArguments[1].Value!;
		var validationAttribute = ValidationAttributeData.FromAttributeData(attributeData);

		return new(true, minimumLength, maximumLength, validationAttribute);
	}
}
