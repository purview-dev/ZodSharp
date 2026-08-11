using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct Base64StringAttributeData(
	bool Exists,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly Base64StringAttributeData Empty = new(
		false,
		ValidationAttributeData.Empty
	);

	public static Base64StringAttributeData FromAttributeData(
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

	public static Base64StringAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.Base64StringAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		var validationAttributeData = ValidationAttributeData.FromAttributeData(attributeData);

		return new(Exists: true, ValidationAttribute: validationAttributeData);
	}
}
