using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct RegularExpressionAttributeData(
	bool Exists,
	string? Pattern,
	int MatchTimeoutInMiniseconds,
	ValidationAttributeData ValidationAttribute
)
{
	public static readonly RegularExpressionAttributeData Empty = new(
		false,
		null,
		0,
		ValidationAttributeData.Empty
	);

	public static RegularExpressionAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributeData
	) => FromAttributeData(attributeData, out _);

	public static RegularExpressionAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributeData,
		out AttributeData? attribute
	)
	{
		attribute = null;
		for (var i = 0; i < attributeData.Length; i++)
		{
			var result = FromAttributeData(attributeData[i]);
			if (result.Exists)
			{
				attribute = attributeData[i];
				return result;
			}
		}

		return Empty;
	}

	public static RegularExpressionAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (
			!TypeLibrary.DataAnnotations.RegularExpressionAttribute.Equals(
				attributeData.AttributeClass
			)
		)
			return Empty;

		attributeData.TryGetConstructorArgument<string>("pattern", out var pattern);
		attributeData.TryGetNamedArgument(
			nameof(MatchTimeoutInMiniseconds),
			out int matchTimeoutInMilliseconds
		);

		var validationAttributeData = ValidationAttributeData.FromAttributeData(attributeData);

		return new(true, pattern, matchTimeoutInMilliseconds, validationAttributeData);
	}
}
