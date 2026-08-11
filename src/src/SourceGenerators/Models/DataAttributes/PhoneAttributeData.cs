using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct PhoneAttribute(bool Exists, ValidationAttributeData ValidationAttribute)
{
	public static readonly PhoneAttribute Empty = new(false, ValidationAttributeData.Empty);

	public static PhoneAttribute FromAttributeData(ImmutableArray<AttributeData> attributes)
	{
		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static PhoneAttribute FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.PhoneAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		var validationAttributeData = ValidationAttributeData.FromAttributeData(attributeData);

		return new(Exists: true, ValidationAttribute: validationAttributeData);
	}
}
