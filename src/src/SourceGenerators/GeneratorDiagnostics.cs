using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators;

static class GeneratorDiagnostics
{
	const string SGCategory = "SourceGenerator";

	public static readonly DiagnosticDescriptor UnhandledException = new(
		id: "ZODSGEN001",
		"Schema generation failed",
		"Failed to generate schema for {0}: {1}",
		category: SGCategory,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor MissingReference = new(
		id: "ZODSGEN002",
		"Missing type reference",
		"Missing reference for type '{0}', add the assembly/ NuGet package",
		category: SGCategory,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidLengthAttributeConfiguration = new(
		id: "ZODSGEN003",
		title: "Invalid LengthAttribute configuration",
		messageFormat: "{0}",
		category: SGCategory,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UnsupportedLengthAttributeTarget = new(
		id: "ZODSGEN004",
		title: "Unsupported LengthAttribute target",
		messageFormat: "{0}",
		category: SGCategory,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidErrorMessageResourceConfiguration = new(
		id: "ZODSGEN005",
		title: "Invalid DataAnnotations error message resource configuration",
		messageFormat: "{0}",
		category: SGCategory,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UnsupportedDataAnnotationsUsage = new(
		id: "ZODSGEN006",
		title: "Unsupported DataAnnotations usage",
		messageFormat: "{0}",
		category: SGCategory,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
}
