using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZodSharp.SourceGenerators.Models;

static class GeneratorDiagnostics
{
	const string Category = "ZodSharp.SourceGenerator";

	public static readonly DiagnosticDescriptor UnhandledException = new(
		id: "ZODSGEN001",
		title: "Fatal error",
		messageFormat: "This is effectively a fatal incident where the source generator has failed",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidLengthAttribute = new(
		id: "ZODSGEN003",
		title: "Invalid LengthAttribute configuration",
		messageFormat: "Invalid configuration for the LengthAttribute on '{0}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UnsupportedLengthAttributeTarget = new(
		id: "ZODSGEN004",
		title: "Unsupported LengthAttribute target",
		messageFormat: "Unsupported LengthAttribute target on '{0}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidDataAnnotationsErrorMessage = new(
		id: "ZODSGEN005",
		title: "Invalid DataAnnotations error message resource configuration",
		messageFormat: "Invalid Data Annotations error message resource configuration on '{0}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UnsupportedDataAnnoationsUsage = new(
		id: "ZODSGEN006",
		title: "Unsupported DataAnnotations usage",
		messageFormat: "Unsupported Data Annotations type on '{0}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static DiagnosticInfo Create(
		DiagnosticDescriptor diagnostic,
		INamedTypeSymbol? symbol = null,
		TypeDeclarationSyntax? declaration = null,
		SyntaxNode? locationNode = null
	)
	{
		var messageArgs = symbol is not null ? new[] { symbol.Name } : [];

		return DiagnosticInfo.Create(
			diagnostic,
			locationNode?.GetLocation() ?? declaration?.Identifier.GetLocation(),
			messageArgs
		);
	}
}
