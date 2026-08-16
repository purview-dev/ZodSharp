using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models;

sealed record SchemaGenerationModel(bool IsSourceGeneratorEnabled, SchemaGenerationContext GenerationContext)
{
	public ImmutableArray<GeneratorResult<ZodSchemaDescriptor>> ZodSchemas { get; set; } = [];

	public ImmutableArray<DiagnosticInfo> Diagnostics { get; set; } = [];
}

sealed record class SchemaGenerationContext : GenerationContext
{
	public SchemaGenerationContext(Compilation compilation, string generatorName, string generatorVersion)
		: base(compilation, generatorName, generatorVersion)
	{
		RequiredAttribute = GetTypeByMetadataName(TypeLibrary.DataAnnotations.RequiredAttribute);
	}

	public INamedTypeSymbol? RequiredAttribute { get; }
}

readonly record struct ZodSchemaDescriptor(
	INamedTypeSymbol Symbol,
	TypeValueObject SchemaType,
	ImmutableArray<ZodPropertyDescriptor> Properties
);

readonly record struct ZodPropertyDescriptor(IPropertySymbol Symbol);
