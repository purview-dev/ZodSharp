using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct EmailAddressAttributeData(
	bool Exists,
	ValidationAttributeData ValidationAttributeData
)
{
	public static readonly EmailAddressAttributeData Empty = new(
		false,
		ValidationAttributeData.Empty
	);

	public static EmailAddressAttributeData FromAttributeData(
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

	public static EmailAddressAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.EmailAddressAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		var validationAttributeData = ValidationAttributeData.FromAttributeData(attributeData);

		return new(true, validationAttributeData);
	}
}
