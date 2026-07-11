using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct MinLengthAttributeData(
	bool Exists,
	int Length,
	string? ErrorMessage,
	string? ErrorMessageResourceName,
	INamedTypeSymbol? ErrorMessageResourceType
)
{
	public static readonly MinLengthAttributeData Empty = new(false, 0, null, null, null);

	public static MinLengthAttributeData FromAttributeData(
		ExecutionContext executionContext,
		ImmutableArray<AttributeData> attributes
	)
	{
		if (executionContext.MinLengthAttribute is null)
			return Empty;

		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(executionContext, attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static MinLengthAttributeData FromAttributeData(
		ExecutionContext executionContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = executionContext.MinLengthAttribute;
		if (
			attributeSymbol is null
			|| !SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol)
		)
		{
			return Empty;
		}

		var length = 0;
		string? errorMessage = null;
		string? errorMessageResourceName = null;
		INamedTypeSymbol? errorMessageResourceType = null;

		if (attributeData.ConstructorArguments.Length == 1 && attributeData.ConstructorArguments[0].Value is int value)
		{
			length = value;
		}

		foreach (var namedArgument in attributeData.NamedArguments)
		{
			if (namedArgument.Key == nameof(ErrorMessage) && namedArgument.Value.Value is string message)
			{
				errorMessage = message;
			}
			else if (
				namedArgument.Key == nameof(ErrorMessageResourceName)
				&& namedArgument.Value.Value is string resourceName
			)
			{
				errorMessageResourceName = resourceName;
			}
			else if (
				namedArgument.Key == nameof(ErrorMessageResourceType)
				&& namedArgument.Value.Value is INamedTypeSymbol resourceType
			)
			{
				errorMessageResourceType = resourceType;
			}
		}

		return new(
			Exists: true,
			Length: length,
			ErrorMessage: errorMessage,
			ErrorMessageResourceName: errorMessageResourceName,
			ErrorMessageResourceType: errorMessageResourceType
		);
	}
}
