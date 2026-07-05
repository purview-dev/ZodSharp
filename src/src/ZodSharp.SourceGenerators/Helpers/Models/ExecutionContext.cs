using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers.Models;

sealed record class ExecutionContext(
	INamedTypeSymbol? ZodSchemaAttribute,
	// Attributes from System.ComponentModel.DataAnnotations
	INamedTypeSymbol? RequiredAttribute,
	INamedTypeSymbol? EmailAddressAttribute,
	INamedTypeSymbol? StringLengthAttribute,
	INamedTypeSymbol? MinLengthAttribute,
	INamedTypeSymbol? MaxLengthAttribute,
	INamedTypeSymbol? RangeAttribute
)
{
	public static bool HasAttribute(IEnumerable<AttributeData> attributes, INamedTypeSymbol? attributeSymbol) =>
		attributeSymbol is not null
		&& attributes.Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));

	public static ExecutionContext Create(Compilation compilation, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		return new(
			ZodSchemaAttribute: compilation.GetTypeByMetadataName(TypeHelpers.ZodSchemaAttributeMetadataName),
			// Attributes from System.ComponentModel.DataAnnotations
			RequiredAttribute: compilation.GetTypeByMetadataName(TypeHelpers.RequiredAttributeMetadataName),
			EmailAddressAttribute: compilation.GetTypeByMetadataName(TypeHelpers.EmailAddressAttributeMetadataName),
			StringLengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.StringLengthAttributeMetadataName),
			MinLengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.MinLengthAttributeMetadataName),
			MaxLengthAttribute: compilation.GetTypeByMetadataName(TypeHelpers.MaxLengthAttributeMetadataName),
			RangeAttribute: compilation.GetTypeByMetadataName(TypeHelpers.RangeAttributeMetadataName)
		);
	}
}
