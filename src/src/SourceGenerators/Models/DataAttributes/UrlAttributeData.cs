using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct UrlAttribute(bool Exists, ValidationAttributeData ValidationAttribute)
{
	public static readonly UrlAttribute Empty = new(false, ValidationAttributeData.Empty);

	public static UrlAttribute FromAttributeData(ImmutableArray<AttributeData> attributes)
	{
		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static UrlAttribute FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.UrlAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		var validationAttributeData = ValidationAttributeData.FromAttributeData(attributeData);

		return new(Exists: true, ValidationAttribute: validationAttributeData);
	}
}
