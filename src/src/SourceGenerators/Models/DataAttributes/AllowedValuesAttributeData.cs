using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct AllowedValuesAttributeData(
	bool Exists,
	ImmutableArray<TypedConstant> Values,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly AllowedValuesAttributeData Empty = new(
		false,
		[],
		ValidationAttributeData.Empty
	);

	public static AllowedValuesAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributes
	) => FromAttributeData(attributes, out _);

	public static AllowedValuesAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributes,
		out AttributeData? attributeData
	)
	{
		attributeData = null;
		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(attributes[i]);

			if (result.Exists)
			{
				attributeData = attributes[i];
				return result;
			}
		}

		return Empty;
	}

	public static AllowedValuesAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (
			!TypeLibrary.DataAnnotations.AllowedValuesAttribute.Equals(attributeData.AttributeClass)
		)
			return Empty;

		var values = attributeData.ConstructorArguments[0].Values;
		var validationAttribute = ValidationAttributeData.FromAttributeData(attributeData);

		return new(true, values, validationAttribute);
	}
}
