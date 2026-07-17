using System.Collections.Immutable;

namespace ZodSharp.SourceGenerators.Models;

sealed record GenerationModel(
	bool IsSourceGeneratorEnabled,
	GenerationContext GenerationContext,
	ImmutableArray<GeneratorResult<TargetSymbolDescriptor>> ZodSchemas,
	ImmutableArray<DiagnosticInfo> Diagnostics
);
