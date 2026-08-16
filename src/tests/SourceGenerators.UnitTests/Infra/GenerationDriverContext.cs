using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Infra;

public sealed record GenerationDriverContext(
	bool IncludeNamespaces = true,
	bool ThrowOnGenerationException = true,
	bool? DisableSourceGenerator = null,
	bool CompileToAssembly = true,
	bool ValidateNoErrorDiagnostics = true,
	bool EnsureValid = true,
	Action<ImmutableArray<MetadataReference>>? PreprocessReferences = null
)
{
	public static readonly GenerationDriverContext Default = new();

	public static readonly GenerationDriverContext DoNotThrowOnGenerationException = new(
		ThrowOnGenerationException: false
	);

	public static readonly GenerationDriverContext Disabled = new(DisableSourceGenerator: true);

	public static readonly GenerationDriverContext WithoutCompilingToAssembly = new(CompileToAssembly: false);

	public static readonly GenerationDriverContext NoCompileOrThrowOnException = new(
		ThrowOnGenerationException: false,
		CompileToAssembly: false
	);

	public static readonly GenerationDriverContext NoNamespaces = new(IncludeNamespaces: false);

	public static readonly GenerationDriverContext IgnoreDiagnostic = new(ValidateNoErrorDiagnostics: false);
}
