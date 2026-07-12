using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct MaxLengthAttributeData(
	bool Exists,
	int Length,
	string? ErrorMessage,
	string? ErrorMessageResourceName,
	INamedTypeSymbol? ErrorMessageResourceType
)
{
	public static readonly MaxLengthAttributeData Empty = new(false, 0, null, null, null);

	public static MaxLengthAttributeData FromAttributeData(
		ExecutionContext executionContext,
		ImmutableArray<AttributeData> attributes
	)
	{
		if (executionContext.MaxLengthAttribute is null)
			return Empty;

		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(executionContext, attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static MaxLengthAttributeData FromAttributeData(
		ExecutionContext executionContext,
		AttributeData attributeData
	)
	{
		var attributeSymbol = executionContext.MaxLengthAttribute;
		if (
			attributeSymbol is null
			|| !SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol)
		)
		{
			return Empty;
		}

		// MaxLengthAttribute() uses -1 to represent the unspecified/
		// database-provider maximum.
		var length = -1;
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
