using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Models;

sealed record SchemaGenerationModel(SchemaGenerationContext GenerationContext, ImmutableArray<GeneratorResult<ZodSchemaDescriptor>> ZodSchemas)
{
	public ImmutableArray<DiagnosticInfo> Diagnostics { get; set; } = [];
}

sealed class SchemaGenerationContext : GenerationContext
{
	public SchemaGenerationContext(Compilation compilation, GenerationSettings settings, ISourceGenLogger? logger)
		: base(compilation, settings, logger)
	{
		RequiredAttribute = GetTypeByMetadataName(TypeLibrary.DataAnnotations.RequiredAttribute);
	}

	public INamedTypeSymbol? RequiredAttribute { get; }
}

// This is recreated outside of the pipeline to avoid the state
// of the CodeWriter being shared across multiple source outputs.
sealed class SchemaGenerationOutputContext(SchemaGenerationContext generationContext) : ISourceGenLogger
{
	public SchemaGenerationContext Generation { get; } = generationContext;

	public CodeWriter Writer { get; private set; } = generationContext.CreateCodeWriter();

	public CodeWriter CreateCodeWriter() => Writer = Generation.CreateCodeWriter();

	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) =>
		Generation.Log(level, indentation, message, args);
}

readonly record struct ZodSchemaDescriptor(
	INamedTypeSymbol Symbol,
	TypeValueObject SchemaType,
	ImmutableArray<ZodPropertyDescriptor> Properties
);

readonly record struct ZodPropertyDescriptor(IPropertySymbol Symbol);
