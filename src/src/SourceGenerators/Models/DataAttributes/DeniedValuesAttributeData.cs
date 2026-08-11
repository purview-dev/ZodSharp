using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct DeniedValuesAttributeData(
	bool Exists,
	ImmutableArray<TypedConstant> Values,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly DeniedValuesAttributeData Empty = new(
		false,
		[],
		ValidationAttributeData.Empty
	);

	public static DeniedValuesAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributes
	) => FromAttributeData(attributes, out _);

	public static DeniedValuesAttributeData FromAttributeData(
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

	public static DeniedValuesAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.DeniedValuesAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		var values = attributeData.ConstructorArguments[0].Values;
		var validationAttribute = ValidationAttributeData.FromAttributeData(attributeData);

		return new(true, values, validationAttribute);
	}
}
