using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Helpers.Models;
using ZodSharp.SourceGenerators.Templates;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

/// <summary>
/// Source generator that creates optimized validators for classes marked with [ZodSchema].
/// Uses IIncrementalGenerator for better performance and incremental compilation support.
/// </summary>
[Generator]
public sealed partial class ZodSchemaGenerator : IIncrementalGenerator
{
	const int EstimatedCodeSize = 2048; // Initial capacity for StringBuilder

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static ctx =>
		{
			// Adds the EmbeddedAttribute definition to the compilation if not already present
			// ensuring that internal generated code doesn't cause visibility issues in referenced
			// assemblies where InternalVisibleTo is set.
			ctx.AddEmbeddedAttributeDefinition();

			ctx.AddSource(
				$"{nameof(TypeHelpers.ZodSchemaAttribute)}.g.cs",
				EmbeddedResources.LoadTemplate(nameof(TypeHelpers.ZodSchemaAttribute))
			);
		});

		var executionContextValueProvicer = context.CompilationProvider.Select(
			static (compilation, cancellationToken) => ExecutionContext.Create(compilation, cancellationToken)
		);
		// Create a syntax provider that finds classes with [ZodSchema] attribute
		var classDeclarations = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				TypeHelpers.ZodSchemaAttribute,
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
				transform: static (ctx, ct) => GetSemanticTargetForGeneration(ctx, ct)
			)
			.Where(static m => m is not null);

		// Combine the target symbols with the execution context to pass both to the source output
		var combined = classDeclarations.Combine(executionContextValueProvicer);

		// Register source output
		context.RegisterSourceOutput(
			combined,
			static (spc, source) =>
			{
				if (source.Left is null)
					return;

				Execute(source.Left, source.Right, spc);
			}
		);
	}

	static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
		node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax;

	static TargetSymbolDescriptor? GetSemanticTargetForGeneration(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		var declaration = (TypeDeclarationSyntax)context.TargetNode;
		return context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol symbol
			? null
			: new(symbol, declaration);
	}
}
