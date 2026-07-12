using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static bool ValidateExecutionContext(ExecutionContext executionContext, SourceProductionContext spc)
	{
		ImmutableArray<Diagnostic> missingReferences = [];

		if (executionContext.ZodSchemaAttribute is null)
			missingReferences = missingReferences.Add(
				Diagnostic.Create(GeneratorDiagnostics.MissingReference, null, TypeHelpers.ZodSchemaAttribute)
			);
		if (executionContext.ImmutableArray is null)
			missingReferences = missingReferences.Add(
				Diagnostic.Create(GeneratorDiagnostics.MissingReference, null, TypeHelpers.ImmutableArrayMetadataName)
			);
		if (executionContext.RequiredAttribute is null)
			missingReferences = missingReferences.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.MissingReference,
					null,
					TypeHelpers.RequiredAttributeMetadataName
				)
			);

		if (!missingReferences.IsEmpty)
			ReportDiagnostics(spc, missingReferences, executionContext);

		return missingReferences.IsEmpty;
	}

	static IncrementalValuesProvider<(
		(Helpers.Models.TargetSymbolDescriptor? TargetDescriptor, bool IsSourceGeneratorDisabled) Left,
		ExecutionContext ExecutionContext
	)> GetGenerationValueProviders(IncrementalGeneratorInitializationContext context, GenerationLogger? logger)
	{
		// Create a syntax provider that finds classes with [ZodSchema] attribute
		var classDeclarations = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				TypeHelpers.ZodSchemaAttribute,
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
				transform: static (ctx, ct) => GetSemanticTargetForGeneration(ctx, ct)
			)
			.Where(static m => m is not null);

		// Collect the execution context, which includes references to required attributes and the logger
		var executionContextValueProvicer = context.CompilationProvider.Select(
			(compilation, cancellationToken) => ExecutionContext.Create(compilation, logger, cancellationToken)
		);

		// Opt-out: set <DisableZodSharpSourceGenerator>true</DisableZodSharpSourceGenerator> to skip generation.
		var isDisabledValueProvider = context.AnalyzerConfigOptionsProvider.Select(
			(opts, _) =>
			{
				opts.GlobalOptions.TryGetValue(TypeHelpers.DisableZodSharpSourceGeneratorProperty, out var val);
				if (bool.TryParse(val, out var isDisabled))
				{
					if (isDisabled)
						logger?.Info("ZodSharpSourceGenerator is disabled via MSBuild property");
				}

				return isDisabled;
			}
		);

		// Combine the target symbols with the execution context to pass both to the source output
		var combined = classDeclarations.Combine(isDisabledValueProvider).Combine(executionContextValueProvicer);

		return combined;
	}
}
