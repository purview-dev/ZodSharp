using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct DisplayAttributeData(
	bool Exists,
	string? ShortName,
	string? Name,
	string? Description,
	string? Prompt,
	ITypeSymbol? ResourceType,
	bool AutoGenerateField,
	bool AutoGenerateFilter,
	int Order
)
{
	public static readonly DisplayAttributeData Empty = new(
		false,
		null,
		null,
		null,
		null,
		null,
		false,
		false,
		0
	);

	public static DisplayAttributeData FromAttributeData(ImmutableArray<AttributeData> attributes)
	{
		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static DisplayAttributeData FromAttributeData(AttributeData attributeData)
	{
		if (!TypeLibrary.DataAnnotations.DisplayAttribute.Equals(attributeData.AttributeClass))
			return Empty;

		var shortName = attributeData.GetNamedArgument<string>(nameof(ShortName));
		var name = attributeData.GetNamedArgument<string>(nameof(Name));
		var description = attributeData.GetNamedArgument<string>(nameof(Description));
		var prompt = attributeData.GetNamedArgument<string>(nameof(Prompt));
		var resourceType = attributeData.GetNamedArgument<ITypeSymbol>(nameof(ResourceType));
		var autoGenerateField = attributeData.GetNamedArgument<bool>(nameof(AutoGenerateField));
		var autoGenerateFilter = attributeData.GetNamedArgument<bool>(nameof(AutoGenerateFilter));
		var order = attributeData.GetNamedArgument<int>(nameof(Order));

		return new(
			true,
			shortName,
			name,
			description,
			prompt,
			resourceType,
			autoGenerateField,
			autoGenerateFilter,
			order
		);
	}
}
