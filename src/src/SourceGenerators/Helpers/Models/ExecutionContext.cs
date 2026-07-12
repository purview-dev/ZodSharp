using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

sealed record class ExecutionContext(
	CodeWriter Writer,
	// ZodSharp symbols
	INamedTypeSymbol? ZodSchemaAttribute,
	// Attributes from System.ComponentModel.DataAnnotations
	INamedTypeSymbol? RequiredAttribute,
	INamedTypeSymbol? EmailAddressAttribute,
	INamedTypeSymbol? StringLengthAttribute,
	INamedTypeSymbol? MinLengthAttribute,
	INamedTypeSymbol? MaxLengthAttribute,
	INamedTypeSymbol? RangeAttribute,
	INamedTypeSymbol? LengthAttribute,
	INamedTypeSymbol? RegularExpressionAttribute,
	INamedTypeSymbol? AllowedValuesAttribute,
	INamedTypeSymbol? DeniedValuesAttribute,
	INamedTypeSymbol? DisplayAttribute,
	// Required Framework Types
	INamedTypeSymbol? ImmutableArray,
	INamedTypeSymbol? ICollection,
	INamedTypeSymbol? IEnumerable,
	INamedTypeSymbol? ICollectionOfT,
	INamedTypeSymbol? IEnumerableOfT,
	INamedTypeSymbol? IReadOnlyCollectionOfT,
	// Debugging support
	GenerationLogger? Logger
)
{
	public static ExecutionContext Create(
		Compilation compilation,
		GenerationLogger? logging,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		return new(
			Writer: new(),
			ZodSchemaAttribute: compilation.GetTypeByMetadataName(TypeHelpers.ZodSchemaAttribute),
			// Attributes from System.ComponentModel.DataAnnotations
			RequiredAttribute: compilation.GetTypeByMetadataName(TypeHelpers.RequiredAttributeMetadataName),
			EmailAddressAttribute: compilation.GetTypeByMetadataName(TypeHelpers.EmailAddressAttributeMetadataName),
			StringLengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.StringLengthAttributeMetadataName),
			MinLengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.MinLengthAttributeMetadataName),
			MaxLengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.MaxLengthAttributeMetadataName),
			RangeAttribute: compilation.GetTypeByMetadataName(TypeHelpers.RangeAttributeMetadataName),
			LengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.LengthAttributeMetadataName),
			RegularExpressionAttribute: compilation.GetTypeByMetadataName(
				TypeHelpers.RegularExpressionAttributeMetadataName
			),
			AllowedValuesAttribute: compilation.GetTypeByMetadataName(TypeHelpers.AllowedValuesAttributeMetadataName),
			DeniedValuesAttribute: compilation.GetTypeByMetadataName(TypeHelpers.DeniedValuesAttributeMetadataName),
			DisplayAttribute: compilation.GetTypeByMetadataName(TypeHelpers.DisplayAttributeMetadataName),
			// Required Framework Types
			ImmutableArray: compilation.GetTypeByMetadataName(TypeHelpers.ImmutableArrayMetadataName),
			ICollection: compilation.GetTypeByMetadataName(TypeHelpers.ICollectionMetadataName),
			IEnumerable: compilation.GetTypeByMetadataName(TypeHelpers.IEnumerableMetadataName),
			ICollectionOfT: compilation.GetTypeByMetadataName(TypeHelpers.ICollectionTMetadataName),
			IEnumerableOfT: compilation.GetTypeByMetadataName(TypeHelpers.IEnumerableTMetadataName),
			IReadOnlyCollectionOfT: compilation.GetTypeByMetadataName(TypeHelpers.IReadOnlyCollectionTMetadataName),
			// Debugging support
			Logger: logging
		);
	}
}
