using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators.Helpers;

static class SourceGenLibrary
{
	public static IncrementalValueProvider<SchemaGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabled = IncrementalPipeline.IsDisabledValueProvider(
			context,
			PropertyLibrary.DisableZodSharpSourceGeneratorProperty
		);
		var generationContext = IncrementalPipeline.GenerationContextValueProvider(
			context,
			(compilation, _) => new SchemaGenerationContext(compilation),
			logger
		);
		var zodSchemas = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.ZodSchemaAttribute,
			predicate: (s, _) =>
				s is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
			transform: (ctx, ct) => GetZodSchemaTargetForGeneration(ctx, logger, ct)
		);

		return isDisabled
			.CombineWith(
				generationContext,
				static (isDisabled, generationContext, _) =>
				{
					SchemaGenerationModel model = new(isDisabled, generationContext);

					List<DiagnosticInfo> diagnostics = [];

					if (diagnostics.Count > 0)
						model.Diagnostics = model.Diagnostics.AddRange(diagnostics);

					return model;
				},
				"CollectSchemaGenerationModel"
			)
			.CollectWith(
				zodSchemas,
				static (model, zodSchemas, _) =>
				{
					model.ZodSchemas = zodSchemas;

					return model;
				},
				"CombineWithZodSchemas"
			);
	}

	static GeneratorResult<ZodSchemaDescriptor> GetZodSchemaTargetForGeneration(
		GeneratorAttributeSyntaxContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		logger?.Debug($"Processing target node: {context.TargetNode.GetType().Name}");

		var declaration = (TypeDeclarationSyntax)context.TargetNode;
		if (
			context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken)
			is not INamedTypeSymbol symbol
		)
			return GeneratorResult<ZodSchemaDescriptor>.Empty;

		ZodSchemaDescriptor result = new(symbol, new(symbol), GetZodProperties(symbol));

		return GeneratorResult<ZodSchemaDescriptor>.Ok(result);
	}

	public static ImmutableArray<ZodPropertyDescriptor> GetZodProperties(INamedTypeSymbol symbol)
	{
		var properties = symbol
			.GetMembers()
			.OfType<IPropertySymbol>()
			.Where(p => p.DeclaredAccessibility == Accessibility.Public)
			.Select(p => new ZodPropertyDescriptor(p));

		return [.. properties];
	}
}
