using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct StringLengthAttribute(
	bool Exists,
	int MaximumLength,
	int MinimumLength,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly StringLengthAttribute Empty = new(
		false,
		int.MaxValue,
		0,
		ValidationAttributeData.Empty
	);

	public static StringLengthAttribute FromAttributeData(
		ImmutableArray<AttributeData> attributes
	) => FromAttributeData(attributes, out _);

	public static StringLengthAttribute FromAttributeData(
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

	public static StringLengthAttribute FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.StringLengthAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		attributeData.TryGetConstructorArgument<int>("maximumLength", out var maximumLength);
		attributeData.TryGetNamedArgument<int>(nameof(MinimumLength), out var minimumLength);
		var validationAttributeData = ValidationAttributeData.FromAttributeData(attributeData);

		return new(
			Exists: true,
			MaximumLength: maximumLength,
			MinimumLength: minimumLength,
			ValidationAttribute: validationAttributeData
		);
	}
}
