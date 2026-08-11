using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct CompareAttributeData(
	bool Exists,
	string OtherProperty,
	string? OtherPropertyDisplayName,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly CompareAttributeData Empty = new(
		false,
		"",
		null,
		ValidationAttributeData.Empty
	);

	public static CompareAttributeData FromAttributeData(ImmutableArray<AttributeData> attributes)
	{
		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static CompareAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.CompareAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		attributeData.TryGetConstructorArgument<string>("otherProperty", out var otherProperty);
		attributeData.TryGetNamedArgument<string>(
			nameof(OtherPropertyDisplayName),
			out var otherPropertyDisplayName
		);

		var validationAttributeData = ValidationAttributeData.FromAttributeData(attributeData);

		return new(
			Exists: true,
			OtherProperty: otherProperty!,
			OtherPropertyDisplayName: otherPropertyDisplayName,
			ValidationAttribute: validationAttributeData
		);
	}
}
