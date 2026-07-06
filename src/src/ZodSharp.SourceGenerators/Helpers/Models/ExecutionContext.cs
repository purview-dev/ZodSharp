using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

sealed record class ExecutionContext(
	// ZodSharp symbols
	INamedTypeSymbol? ZodSchemaAttribute,
	// Attributes from System.ComponentModel.DataAnnotations
	INamedTypeSymbol? RequiredAttribute,
	INamedTypeSymbol? EmailAddressAttribute,
	INamedTypeSymbol? StringLengthAttribute,
	INamedTypeSymbol? MinLengthAttribute,
	INamedTypeSymbol? MaxLengthAttribute,
	INamedTypeSymbol? RangeAttribute,
	// Required Framework Types
	INamedTypeSymbol? ImmutableArray,
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
			ZodSchemaAttribute: compilation.GetTypeByMetadataName(TypeHelpers.ZodSchemaAttribute),
			// Attributes from System.ComponentModel.DataAnnotations
			RequiredAttribute: compilation.GetTypeByMetadataName(TypeHelpers.RequiredAttributeMetadataName),
			EmailAddressAttribute: compilation.GetTypeByMetadataName(TypeHelpers.EmailAddressAttributeMetadataName),
			StringLengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.StringLengthAttributeMetadataName),
			MinLengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.MinLengthAttributeMetadataName),
			MaxLengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.MaxLengthAttributeMetadataName),
			RangeAttribute: compilation.GetTypeByMetadataName(TypeHelpers.RangeAttributeMetadataName),
			// Required Framework Types
			ImmutableArray: compilation.GetTypeByMetadataName(TypeHelpers.ImmutableArrayMetadataName),
			// Debugging support
			Logger: logging
		);
	}
}
