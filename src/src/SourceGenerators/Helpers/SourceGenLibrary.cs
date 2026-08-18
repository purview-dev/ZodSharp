using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators.Helpers;

static class SourceGenLibrary
{
	public static IncrementalValueProvider<SchemaGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context
	)
	{
		var outputContext = IncrementalPipeline.GenerationContextValueProvider<SchemaGenerationContext>(
			context,
			typeof(ZodSchemaGenerator).FullName,
			AssemblyInfo.Version,
			(compilation, generatorSettings, logger, _) => new(compilation, generatorSettings, logger),
			PropertyLibrary.DisableZodSharpSourceGeneratorProperty
		);
		var zodSchemas = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.ZodSchemaAttribute,
			predicate: static (s, _) =>
				s is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
			transform: static (ctx, ct) => GetZodSchemaTargetForGeneration(ctx, ct)
		);

		return outputContext.CollectWith(
			zodSchemas,
			static (generationContext, zodSchemas, _) => new SchemaGenerationModel(generationContext, zodSchemas),
			"CollectZodSchemas"
		);
	}

	static GeneratorResult<ZodSchemaDescriptor> GetZodSchemaTargetForGeneration(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		var declaration = (TypeDeclarationSyntax)context.TargetNode;
		if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol symbol)
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
