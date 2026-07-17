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

	public static readonly DiagnosticDescriptor CustomValidationMethodNotFound = new(
		id: "ZODSGEN007",
		title: "Custom validation method not found",
		messageFormat: "Custom validation method '{0}' was configured for schema type '{1}', but no matching method was found.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationInvalidReturnType = new(
		id: "ZODSGEN008",
		title: "Invalid custom validation method return type",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' must return 'ValueTask<ValidationResult<{1}>>'.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationInvalidParameterCount = new(
		id: "ZODSGEN009",
		title: "Invalid custom validation method parameter count",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' must accept exactly two parameters: (T value, CancellationToken cancellationToken).",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationInvalidModelParameter = new(
		id: "ZODSGEN010",
		title: "Invalid custom validation method model parameter",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' must have the schema model type as its first parameter.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationInvalidCancellationToken = new(
		id: "ZODSGEN011",
		title: "Invalid custom validation method cancellation token parameter",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' must have 'CancellationToken' as its second parameter.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationGenericMethod = new(
		id: "ZODSGEN012",
		title: "Generic custom validation method unsupported",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' must not be generic.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationInvalidStaticInstance = new(
		id: "ZODSGEN013",
		title: "Invalid custom validation method static/instance form",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' must be static because the generated validator is a separate type.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationInaccessible = new(
		id: "ZODSGEN014",
		title: "Inaccessible custom validation method",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' is not accessible from the generated validator.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationAmbiguousOverloads = new(
		id: "ZODSGEN015",
		title: "Ambiguous custom validation method overloads",
		messageFormat: "Multiple custom validation methods named '{0}' match the required signature for schema type '{1}'.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationInvalidMethodName = new(
		id: "ZODSGEN016",
		title: "Invalid custom validation method name",
		messageFormat: "Custom validation method name '{0}' configured for schema type '{1}' is not a valid C# identifier.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationAbstractMethod = new(
		id: "ZODSGEN017",
		title: "Abstract custom validation method unsupported",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' must not be abstract.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationUnimplementedPartial = new(
		id: "ZODSGEN018",
		title: "Unimplemented partial custom validation method",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' is an unimplemented partial method.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor CustomValidationInvalidParameterModifier = new(
		id: "ZODSGEN019",
		title: "Invalid custom validation method parameter modifier",
		messageFormat: "Custom validation method '{0}' on schema type '{1}' must not use ref, in, out, params, or scoped parameters.",
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
