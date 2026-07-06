using System.Globalization;
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
public sealed partial class ZodSchemaGenerator : IIncrementalGenerator, ILogSupport
{
	const int EstimatedCodeSize = 2048; // Initial capacity for StringBuilder
	GenerationLogger? _logger;

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

		var generationValueProviders = GetGenerationValueProviders(context, _logger);

		// Register source outputs
		context.RegisterSourceOutput(
			generationValueProviders,
			static (spc, source) =>
			{
				if (source.Left.IsSourceGeneratorDisabled)
					return;

				if (source.Left.TargetDescriptor is null)
					return;

				if (!ValidateExecutionContext(source.ExecutionContext, spc))
					return;

				Execute(source.Left.TargetDescriptor, source.ExecutionContext, spc);
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

	static void ReportDiagnostics(
		SourceProductionContext context,
		Diagnostic diagnostic,
		ExecutionContext executionContext
	) => ReportDiagnostics(context, [diagnostic], executionContext.Logger);

	static void ReportDiagnostics(
		SourceProductionContext context,
		IEnumerable<Diagnostic> diagnostics,
		ExecutionContext executionContext
	) => ReportDiagnostics(context, diagnostics, executionContext.Logger);

	static void ReportDiagnostics(SourceProductionContext context, Diagnostic diagnostic, GenerationLogger? logger) =>
		ReportDiagnostics(context, [diagnostic], logger);

	static void ReportDiagnostics(
		SourceProductionContext context,
		IEnumerable<Diagnostic> diagnostics,
		GenerationLogger? logger
	)
	{
		foreach (var diagnostic in diagnostics)
		{
			context.ReportDiagnostic(diagnostic);

			logger?.Diagnostic(diagnostic.GetMessage(CultureInfo.InvariantCulture));
		}
	}

	void ILogSupport.SetLogOutput(Action<string, OutputType> action) => _logger = new GenerationLogger(action);
}
