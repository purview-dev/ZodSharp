using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators;

/// <summary>
/// Reports when a validation rule (a type implementing <c>ZodSharp.Core.IValidationRule&lt;T&gt;</c>) does not
/// expose its error identity as public constants. Every rule is expected to declare
/// <c>public const string ErrorCode</c> and <c>public const string MessageFormat</c> so that a test (or a
/// consumer) can assert against the rule rather than duplicating the literal code and message.
/// </summary>
/// <remarks>
/// The check applies to source-declared rules only; the built-in rules in the referenced assembly are
/// metadata and are therefore ignored. A rule that inherits the constants from a base class satisfies the
/// convention, and abstract rule bases are skipped so that a shared base can host the constants for its
/// concrete derivations. Types that do not implement the rule interface are never considered.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidationRuleConventionsAnalyzer : DiagnosticAnalyzer
{
	/// <summary>The diagnostic id for a rule that does not expose its error identity constants.</summary>
	public const string DiagnosticId = "ZODSGEN042";

	/// <summary>The name of the error-code constant a rule should expose.</summary>
	public const string ErrorCodeConstantName = "ErrorCode";

	/// <summary>The name of the message-format constant a rule should expose.</summary>
	public const string MessageFormatConstantName = "MessageFormat";

	static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsList =
	[
		DiagnosticLibrary.RuleMissingErrorIdentityConstants,
	];

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsList;

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
	}

	static void AnalyzeType(SymbolAnalysisContext context)
	{
		if (context.Symbol is not INamedTypeSymbol type)
			return;

		// Only concrete classes and structs are rules a consumer actually instantiates; interfaces, enums
		// and abstract bases never carry the constants themselves.
		if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.IsAbstract || type.IsImplicitlyDeclared)
			return;

		if (!IsValidationRule(type))
			return;

		var location = GetLocation(type);
		if (location is null)
			return;

		var missing = ImmutableArray.CreateBuilder<string>(2);
		if (!HasPublicStringConstant(type, ErrorCodeConstantName))
			missing.Add(ErrorCodeConstantName);
		if (!HasPublicStringConstant(type, MessageFormatConstantName))
			missing.Add(MessageFormatConstantName);

		if (missing.Count == 0)
			return;

		context.ReportDiagnostic(
			Diagnostic.Create(
				DiagnosticLibrary.RuleMissingErrorIdentityConstants,
				location,
				type.Name,
				DescribeMissing(missing)
			)
		);
	}

	static bool IsValidationRule(INamedTypeSymbol type) =>
		type.AllInterfaces.Any(static @interface =>
			@interface.OriginalDefinition is { Name: "IValidationRule", Arity: 1 } definition
			&& definition.ContainingNamespace.ToDisplayString() == TypeLibraryGenerator.ZodSharpCoreNamespace
		);

	/// <summary>
	/// Returns <see langword="true"/> when a public <c>const string</c> field named <paramref name="name"/>
	/// is reachable on the type or one of its base types.
	/// </summary>
	static bool HasPublicStringConstant(INamedTypeSymbol type, string name)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			foreach (var member in current.GetMembers(name))
			{
				if (
					member is IFieldSymbol { IsConst: true, DeclaredAccessibility: Accessibility.Public } field
					&& field.Type.SpecialType == SpecialType.System_String
				)
				{
					return true;
				}
			}
		}

		return false;
	}

	static string DescribeMissing(ImmutableArray<string>.Builder missing) =>
		missing.Count == 1
			? $"a public '{missing[0]}' constant"
			: $"public '{missing[0]}' and '{missing[1]}' constants";

	static Location? GetLocation(INamedTypeSymbol type)
	{
		foreach (var location in type.Locations)
		{
			if (location.IsInSource)
				return location;
		}

		return null;
	}
}
