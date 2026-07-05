using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

readonly record struct StringLengthAttribute(bool Exists, int MaximumLength, int MinimumLength, string? ErrorMessage)
{
	public static readonly StringLengthAttribute Empty = new(false, int.MaxValue, 0, null);

	public static StringLengthAttribute FromAttributeData(
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

	public static StringLengthAttribute FromAttributeData(
		ExecutionContext executionContext,
		AttributeData attributeData
	)
	{
		var exists =
			executionContext.StringLengthAttribute is not null
			&& SymbolEqualityComparer.Default.Equals(
				attributeData?.AttributeClass,
				executionContext.StringLengthAttribute
			);
		var maximumLength = int.MaxValue;
		var minimumLength = 0;
		var errorMessage = (string?)null;
		if (exists)
		{
			if (
				attributeData!
					.ConstructorArguments.FirstOrDefault(arg =>
						string.Equals(arg.Type?.Name, nameof(MaximumLength), StringComparison.OrdinalIgnoreCase)
					)
					.Value
				is int maxLenArg
			)
				maximumLength = maxLenArg;

			foreach (var namedArg in attributeData!.NamedArguments)
			{
				if (namedArg.Key == nameof(MaximumLength) && namedArg.Value.Value is int maxLen)
					maximumLength = maxLen;
				else if (namedArg.Key == nameof(MinimumLength) && namedArg.Value.Value is int minLen)
					minimumLength = minLen;
				else if (namedArg.Key == nameof(ErrorMessage) && namedArg.Value.Value is string errorMsg)
					errorMessage = errorMsg;
			}
		}

		return new(exists, maximumLength, minimumLength, errorMessage);
	}
}
