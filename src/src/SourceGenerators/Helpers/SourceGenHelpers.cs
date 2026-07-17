using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators.Helpers;

static class SourceGenHelpers
{
	public static IncrementalValueProvider<GenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabledValueProvider = IsSourceGeneratorDisabledValueProvider(context, logger);
		var generationContextValueProvider = GetGeneratorValueProvider(context, logger);
		var zodSchemaValueProvider = GetGenerationValueProviders(context, TypeHelpers.ZodSchemaAttribute, logger);

		return isDisabledValueProvider
			.Combine(generationContextValueProvider) // (bool, GenerationContext)
			.Combine(zodSchemaValueProvider.Collect()) // ((bool, GenerationContext), ImmutableArray<T>)
			.Select(
				static (nested, _) =>
				{
					var ((isDisabled, generationContext), zodSchemas) = nested;
					var hasMultipleHostApps = zodSchemas.Length > 1;

					return new GenerationModel(
						IsSourceGeneratorEnabled: !isDisabled,
						GenerationContext: generationContext,
						ZodSchemas: zodSchemas, []
					);
				}
			);
	}

	static IncrementalValuesProvider<GeneratorResult<TargetSymbolDescriptor>> GetGenerationValueProviders(
		IncrementalGeneratorInitializationContext context,
		string fullAttributeName,
		GenerationLogger? logger
	)
	{
		// Create a syntax provider that finds classes with the specified attribute.
		var targetSymbols = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				fullAttributeName,
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
				transform: (ctx, ct) => GetSemanticTargetForGeneration(ctx, fullAttributeName, ct)
			)
			.WithTrackingName($"Get{fullAttributeName}Targets");

		return targetSymbols;

		// We only want to consider class declarations for generation, so we filter the syntax nodes accordingly.
		static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax;

		static GeneratorResult<TargetSymbolDescriptor> GetSemanticTargetForGeneration(
			GeneratorAttributeSyntaxContext context,
			string fullAttributeName,
			CancellationToken cancellationToken
		)
		{
			var declaration = (TypeDeclarationSyntax)context.TargetNode;
			if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol symbol)
				return GeneratorResult<TargetSymbolDescriptor>.Empty;

			TargetSymbolDescriptor result = new(
				symbol,
				declaration
			);

			return GeneratorResult<TargetSymbolDescriptor>.Ok(result);
		}
	}

	static IncrementalValueProvider<GenerationContext> GetGeneratorValueProvider(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		// Collect the generation context, which includes references to required attributes and the logger
		var generationContextValueProvider = context
			.CompilationProvider.Select(
				(compilation, cancellationToken) => GenerationContext.Create(compilation, logger, cancellationToken)
			)
			.WithTrackingName("GetGenerationContext");

		return generationContextValueProvider;
	}

	static IncrementalValueProvider<bool> IsSourceGeneratorDisabledValueProvider(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		// Opt-out: set <DisableZodSharpSourceGenerator>true</DisableZodSharpSourceGenerator> to skip generation.
		var isDisabledValueProvider = context
			.AnalyzerConfigOptionsProvider.Select(
				(opts, _) =>
				{
					opts.GlobalOptions.TryGetValue(
						TypeHelpers.DisableZodSharpSourceGeneratorProperty,
						out var val
					);
					if (bool.TryParse(val, out var isDisabled))
					{
						if (isDisabled)
							logger?.Info(
								"ZodSharp source generators are disabled via MSBuild property"
							);
					}

					return isDisabled;
				}
			)
			.WithTrackingName("IsSourceGeneratorDisabled");

		return isDisabledValueProvider;
	}
}
