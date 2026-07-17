using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

readonly record struct RangeAttributeData(
	bool Exists,
	RangeAttributeKind Kind,
	object? Minimum,
	object? Maximum,
	ITypeSymbol? OperandType,
	bool MinimumIsExclusive,
	bool MaximumIsExclusive,
	bool ConvertValueInInvariantCulture,
	bool ParseLimitsInInvariantCulture,
	string? ErrorMessage,
	string? ErrorMessageResourceName,
	INamedTypeSymbol? ErrorMessageResourceType
)
{
	public static readonly RangeAttributeData Empty = new(
		false,
		RangeAttributeKind.None,
		null,
		null,
		null,
		false,
		false,
		false,
		false,
		null,
		null,
		null
	);

	public static RangeAttributeData FromAttributeData(
		GenerationContext generationContext,
		ImmutableArray<AttributeData> attributes
	)
	{
		if (generationContext.RangeAttribute is null)
			return Empty;

		for (var i = 0; i < attributes.Length; i++)
		{
			var result = FromAttributeData(generationContext, attributes[i]);

			if (result.Exists)
				return result;
		}

		return Empty;
	}

	public static RangeAttributeData FromAttributeData(GenerationContext generationContext, AttributeData attributeData)
	{
		var rangeAttributeSymbol = generationContext.RangeAttribute;
		if (
			rangeAttributeSymbol is null
			|| !SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, rangeAttributeSymbol)
		)
		{
			return Empty;
		}

		var constructorArguments = attributeData.ConstructorArguments;

		if (constructorArguments.Length is not (2 or 3))
			return Empty with { Exists = true, ErrorMessage = "RangeAttribute has an unsupported constructor shape." };

		var kind = RangeAttributeKind.None;
		object? minimum = null;
		object? maximum = null;
		ITypeSymbol? operandType = null;

		switch (constructorArguments.Length)
		{
			// Min + Max
			case 2:
				ReadNumericRange(constructorArguments, ref kind, ref minimum, ref maximum, ref operandType);
				break;
			// OperandType + Min + Max
			case 3:
				ReadConvertedRange(constructorArguments, ref kind, ref minimum, ref maximum, ref operandType);
				break;
		}

		var minimumIsExclusive = false;
		var maximumIsExclusive = false;
		var convertValueInInvariantCulture = false;
		var parseLimitsInInvariantCulture = false;
		string? errorMessage = null;
		string? errorMessageResourceName = null;
		INamedTypeSymbol? errorMessageResourceType = null;

		foreach (var namedArgument in attributeData.NamedArguments)
		{
			switch (namedArgument.Key)
			{
				case nameof(MinimumIsExclusive) when namedArgument.Value.Value is bool value:
					minimumIsExclusive = value;
					break;

				case nameof(MaximumIsExclusive) when namedArgument.Value.Value is bool value:
					maximumIsExclusive = value;
					break;

				case nameof(ConvertValueInInvariantCulture) when namedArgument.Value.Value is bool value:
					convertValueInInvariantCulture = value;
					break;

				case nameof(ParseLimitsInInvariantCulture) when namedArgument.Value.Value is bool value:
					parseLimitsInInvariantCulture = value;
					break;

				case nameof(ErrorMessage) when namedArgument.Value.Value is string value:
					errorMessage = value;
					break;

				case nameof(ErrorMessageResourceName) when namedArgument.Value.Value is string value:
					errorMessageResourceName = value;
					break;

				case nameof(ErrorMessageResourceType) when namedArgument.Value.Value is INamedTypeSymbol value:
					errorMessageResourceType = value;
					break;
			}
		}

		if (kind == RangeAttributeKind.None)
		{
			return Empty with
			{
				Exists = true,
				ErrorMessage = errorMessage ?? "RangeAttribute contains unsupported range values.",
			};
		}

		// Success..!!
		return new(
			Exists: true,
			Kind: kind,
			Minimum: minimum,
			Maximum: maximum,
			OperandType: operandType,
			MinimumIsExclusive: minimumIsExclusive,
			MaximumIsExclusive: maximumIsExclusive,
			ConvertValueInInvariantCulture: convertValueInInvariantCulture,
			ParseLimitsInInvariantCulture: parseLimitsInInvariantCulture,
			ErrorMessage: errorMessage,
			ErrorMessageResourceName: errorMessageResourceName,
			ErrorMessageResourceType: errorMessageResourceType
		);
	}

	static void ReadNumericRange(
		ImmutableArray<TypedConstant> arguments,
		ref RangeAttributeKind kind,
		ref object? minimum,
		ref object? maximum,
		ref ITypeSymbol? operandType
	)
	{
		var minimumArgument = arguments[0];
		var maximumArgument = arguments[1];

		if (minimumArgument.Value is int minimumValue && maximumArgument.Value is int maximumValue)
		{
			kind = RangeAttributeKind.Int32;
			minimum = minimumValue;
			maximum = maximumValue;
			operandType = minimumArgument.Type;
			return;
		}

		if (minimumArgument.Value is double minimumValueDouble && maximumArgument.Value is double maximumValueDouble)
		{
			kind = RangeAttributeKind.Double;
			minimum = minimumValueDouble;
			maximum = maximumValueDouble;
			operandType = minimumArgument.Type;
		}
	}

	static void ReadConvertedRange(
		ImmutableArray<TypedConstant> arguments,
		ref RangeAttributeKind kind,
		ref object? minimum,
		ref object? maximum,
		ref ITypeSymbol? operandType
	)
	{
		if (
			arguments[0].Value is not ITypeSymbol type
			|| arguments[1].Value is not string minimumValue
			|| arguments[2].Value is not string maximumValue
		)
		{
			return;
		}

		kind = RangeAttributeKind.Converted;
		operandType = type;
		minimum = minimumValue;
		maximum = maximumValue;
	}
}
