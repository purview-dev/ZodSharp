using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct MaxLengthAttributeData(bool Exists, int Length, string? ErrorMessage)
{
	public static readonly MaxLengthAttributeData Empty = new(false, 0, null);

	public static MaxLengthAttributeData FromAttributeData(
		ExecutionContext executionContext,
		ImmutableArray<AttributeData> attributeData
	)
	{
		if (executionContext.RequiredAttribute is not null)
		{
			for (var i = 0; i < attributeData.Length; i++)
			{
				var result = FromAttributeData(executionContext, attributeData[i]);
				if (result.Exists)
					return result;
			}
		}

		return Empty;
	}

	public static MaxLengthAttributeData FromAttributeData(
		ExecutionContext executionContext,
		AttributeData attributeData
	)
	{
		var exists =
			executionContext.MaxLengthAttribute is not null
			&& SymbolEqualityComparer.Default.Equals(
				attributeData?.AttributeClass,
				executionContext.MaxLengthAttribute
			);
		var length = 0;
		var errorMessage = (string?)null;
		if (exists)
		{
			if (
				attributeData!
					.ConstructorArguments.FirstOrDefault(arg =>
						string.Equals(arg.Type?.Name, nameof(Length), StringComparison.OrdinalIgnoreCase)
					)
					.Value
				is int maxLenArg
			)
				length = maxLenArg;

			foreach (var namedArg in attributeData!.NamedArguments)
			{
				if (namedArg.Key == nameof(Length) && namedArg.Value.Value is int len)
					length = len;
				else if (namedArg.Key == nameof(ErrorMessage) && namedArg.Value.Value is string errorMsg)
					errorMessage = errorMsg;
			}
		}

		return new(exists, length, errorMessage);
	}
}
