using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models;

readonly record struct ZodSchemaAttributeData(
	bool Exists,
	string? SchemaName,
	bool GenerateValidateMethod,
	bool GenerateParseMethod,
	bool EnableComposition,
	string? CustomValidationMethodName
)
{
	public static readonly ZodSchemaAttributeData Empty = new(false, null, true, true, true, null);

	public static ZodSchemaAttributeData FromAttributeData(
		ImmutableArray<AttributeData> attributes
	) => FromAttributeData(attributes, out _);

	public static ZodSchemaAttributeData FromAttributeData(
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

	public static ZodSchemaAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.ZodSchemaAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		var schemaName = attributeData.GetNamedArgument<string>(nameof(SchemaName));
		var generateValidateMethod = attributeData.GetNamedArgument(
			nameof(GenerateValidateMethod),
			true
		);
		var generateParseMethod = attributeData.GetNamedArgument(nameof(GenerateParseMethod), true);
		var enableComposition = attributeData.GetNamedArgument(nameof(EnableComposition), true);
		var customValidationMethodName = attributeData.GetNamedArgument<string>(
			nameof(CustomValidationMethodName)
		);

		return new(
			true,
			schemaName,
			generateValidateMethod,
			generateParseMethod,
			enableComposition,
			customValidationMethodName
		);
	}
}
