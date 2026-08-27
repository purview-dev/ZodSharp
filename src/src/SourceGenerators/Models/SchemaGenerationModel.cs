using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Models;

sealed record SchemaGenerationModel(GenerationContext<SchemaGenerationCapabilities> Context)
{
	public EquatableArray<GeneratorResult<ZodSchemaDescriptor>> ZodSchemas { get; init; } = [];

	public EquatableArray<DiagnosticInfo> Diagnostics { get; init; } = [];
}

sealed record SchemaGenerationCapabilities(Compilation Compilation) : IGenerationCapabilities
{
	public bool HasRequiredAttribute { get; init; }
}

// This is recreated outside of the pipeline to avoid the state
// of the CodeWriter being shared across multiple source outputs.
sealed record SchemaGenerationOutputContext(GenerationContext<SchemaGenerationCapabilities> Context) : ISourceGenLogger
{
	public CodeWriter Writer { get; private set; } = Context.CreateCodeWriter();

	public CodeWriter CreateCodeWriter() => Writer = Context.CreateCodeWriter();

	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) =>
		Context.Log(level, indentation, message, args);
}

readonly record struct ZodSchemaDescriptor(TypeIdentity SchemaType, string DeclarationFingerprint, bool IsPrimary);

readonly record struct SchemaSet(EquatableArray<ZodSchemaDescriptor> Schemas);
