using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators.Helpers;

/// <summary>
/// Discovers and validates the <c>OnZodValidate</c> refinement hook on a <c>[ZodSchema]</c> target.
/// </summary>
/// <remarks>
/// <para>
/// The hook is a generator-declared partial method —
/// <c>partial void OnZodValidate(RefineCtx&lt;T&gt; context)</c> — so the IDE offers the implementation
/// with the correct signature and the name is never resolved by convention.
/// </para>
/// <para>
/// Roslyn does not surface a method symbol for a partial method implementation that has no declaration,
/// so discovery and validation are syntax based (the same approach the <c>Purview.ValueObjects</c>
/// generator uses). A malformed hook is reported rather than bound, which keeps the emitted validator
/// from referencing a member the compiler would reject.
/// </para>
/// </remarks>
static class ZodRefinementHookResolver
{
	/// <summary>
	/// Discovers the <c>OnZodValidate</c> refinement hook on a <c>[ZodSchema]</c> target and returns an
	/// immutable <see cref="ZodRefinementHookData"/> describing how the generated schema declares and
	/// invokes it.
	/// </summary>
	internal static GeneratorResult<ZodRefinementHookData> Resolve(INamedTypeSymbol classSymbol)
	{
		// A body-less partial declaration is the one the generator itself emits into the caller's type. In a real
		// compilation analyzers run after generation, so that declaration is visible here and must not be
		// mistaken for a malformed implementation.
		var userDeclarations = GetHookDeclarations(classSymbol)
			.Where(static declaration => !IsGeneratedHookDeclaration(declaration))
			.ToArray();
		var isImplemented = userDeclarations.Any(IsValidHookImplementation);
		var hasMalformedDeclaration = userDeclarations.Any(declaration => !IsValidHookImplementation(declaration));
		var chainIsPartial = IsPartialWithContainingTypes(classSymbol);
		var diagnostics = ImmutableArray.CreateBuilder<ReportableDiagnostic>();

		if (hasMalformedDeclaration)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.ZodRefinementHookInvalidSignature,
					true,
					userDeclarations[0].Identifier.GetLocation(),
					TypeLibraryGenerator.ZodRefinementHookName,
					classSymbol.Name
				)
			);
		}

		if (isImplemented && !chainIsPartial)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.ZodRefinementHookTypeNotPartial,
					true,
					GetHookLocation(classSymbol) ?? GetTypeLocation(classSymbol),
					TypeLibraryGenerator.ZodRefinementHookName,
					classSymbol.Name
				)
			);
		}

		// The retired contract — an instance Validate method returning IEnumerable<ValidationError> — is no
		// longer bound, so report it rather than silently ignoring the rules it declares.
		foreach (var retiredMethod in GetRetiredRefinementMethods(classSymbol))
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.SyncRefinementMethodRetired,
					false,
					retiredMethod.Locations.FirstOrDefault(static location => location.IsInSource)
						?? GetTypeLocation(classSymbol),
					retiredMethod.Name,
					classSymbol.Name
				)
			);
		}

		if (hasMalformedDeclaration || (isImplemented && !chainIsPartial))
		{
			return GeneratorResult<ZodRefinementHookData>.Create(ZodRefinementHookData.None, diagnostics.ToImmutable());
		}

		// The declaration is emitted whenever the chain can be reopened as partial, so the IDE can offer
		// the implementation. The call site is emitted only when a body actually exists.
		var hook = chainIsPartial
			? new ZodRefinementHookData(isImplemented, BuildDeclarationChain(classSymbol))
			: ZodRefinementHookData.None;

		return GeneratorResult<ZodRefinementHookData>.Create(hook, diagnostics.ToImmutable());
	}

	/// <summary>
	/// Builds the type declarations a generated hook file must reopen: the containing types, outermost
	/// first, followed by the target type itself.
	/// </summary>
	/// <remarks>
	/// Generated attributes are suppressed so reopening the caller's own type does not decorate it with
	/// generator markers.
	/// </remarks>
	static EquatableArray<TypeDeclarationOptions> BuildDeclarationChain(INamedTypeSymbol typeSymbol)
	{
		Stack<INamedTypeSymbol> containingTypes = new();
		for (var current = typeSymbol.ContainingType; current is not null; current = current.ContainingType)
			containingTypes.Push(current);

		var chain = ImmutableArray.CreateBuilder<TypeDeclarationOptions>();
		while (containingTypes.Count > 0)
		{
			chain.Add(
				TypeHelpers.CreatePartialTypeDeclarationOptions(containingTypes.Pop()) with
				{
					IncludeGeneratedAttributes = false,
				}
			);
		}

		chain.Add(
			TypeHelpers.CreatePartialTypeDeclarationOptions(typeSymbol) with
			{
				IncludeGeneratedAttributes = false,
			}
		);

		return new(chain.ToImmutable());
	}

	/// <summary>
	/// True when the target type and every containing type is declared <c>partial</c> (and none is
	/// file-local, since a file-local type cannot be reopened from a generated file).
	/// </summary>
	static bool IsPartialWithContainingTypes(INamedTypeSymbol typeSymbol)
	{
		for (var current = typeSymbol; current is not null; current = current.ContainingType)
		{
			if (current.IsFileLocal)
				return false;

			var isPartial = current
				.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
				.OfType<TypeDeclarationSyntax>()
				.Any(declaration => declaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));

			if (!isPartial)
				return false;
		}

		return true;
	}

	/// <summary>
	/// True for a hook declaration that matches the generated signature: <c>partial</c>, non-<c>static</c>,
	/// non-<c>abstract</c>, returning <c>void</c>, taking a single unmodified <c>RefineCtx&lt;T&gt;</c>
	/// parameter, and supplying a body.
	/// </summary>
	/// <remarks>
	/// The parameter's type argument is checked by the compiler against the generated declaration, so only
	/// the shape the emitted call site depends on is validated here.
	/// </remarks>
	static bool IsValidHookImplementation(MethodDeclarationSyntax method) =>
		method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword))
		&& !method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword))
		&& !method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword))
		&& (method.Body is not null || method.ExpressionBody is not null)
		&& IsVoidReturnType(method)
		&& method.ParameterList.Parameters.Count == 1
		&& IsRefineContextParameter(method.ParameterList.Parameters[0]);

	static bool IsVoidReturnType(MethodDeclarationSyntax method) =>
		method.ReturnType is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);

	/// <summary>
	/// True for the body-less <c>partial</c> declaration the generator emits into the target type. It carries no
	/// implementation, and a caller cannot write it by hand (it would collide with the generated declaration).
	/// </summary>
	static bool IsGeneratedHookDeclaration(MethodDeclarationSyntax method) =>
		method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword))
		&& method.Body is null
		&& method.ExpressionBody is null;

	static bool IsRefineContextParameter(ParameterSyntax parameter)
	{
		// Rejects ref/in/out/scoped/params/this — the generated declaration takes a plain by-value context.
		if (parameter.Modifiers.Count > 0 || parameter.Type is NullableTypeSyntax)
			return false;

		var generic = parameter.Type switch
		{
			GenericNameSyntax direct => direct,
			QualifiedNameSyntax { Right: GenericNameSyntax qualified } => qualified,
			_ => null,
		};

		return generic is not null
			&& generic.Identifier.Text == TypeLibraryGenerator.ZodRefineContextName
			&& generic.TypeArgumentList.Arguments.Count == 1;
	}

	static MethodDeclarationSyntax[] GetHookDeclarations(INamedTypeSymbol typeSymbol) =>
		[
			.. typeSymbol
				.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
				.OfType<TypeDeclarationSyntax>()
				.SelectMany(declaration => declaration.Members.OfType<MethodDeclarationSyntax>())
				.Where(method => method.Identifier.Text == TypeLibraryGenerator.ZodRefinementHookName),
		];

	static Location? GetHookLocation(INamedTypeSymbol typeSymbol) =>
		GetHookDeclarations(typeSymbol).FirstOrDefault(IsValidHookImplementation)?.Identifier.GetLocation();

	static Location GetTypeLocation(INamedTypeSymbol typeSymbol) =>
		typeSymbol.Locations.FirstOrDefault(static location => location.IsInSource) ?? Location.None;

	/// <summary>
	/// Finds members that still declare the retired synchronous refinement contract: an accessible instance
	/// method named <c>Validate</c> returning <c>IEnumerable&lt;ValidationError&gt;</c>, taking either no
	/// parameters or a single <c>RefineCtx&lt;T&gt;</c>.
	/// </summary>
	static IEnumerable<IMethodSymbol> GetRetiredRefinementMethods(INamedTypeSymbol typeSymbol) =>
		typeSymbol
			.GetMembers(TypeLibraryGenerator.RetiredSyncRefinementMethodName)
			.OfType<IMethodSymbol>()
			.Where(method =>
				!method.IsStatic
				&& !method.IsImplicitlyDeclared
				&& method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
				&& method.Parameters.Length <= 1
				&& ReturnsEnumerableOfValidationError(method)
				&& (method.Parameters.Length == 0 || IsRefineContextOf(method.Parameters[0].Type, typeSymbol))
			);

	static bool IsRefineContextOf(ITypeSymbol type, INamedTypeSymbol classSymbol)
	{
		if (
			type is not INamedTypeSymbol named
			|| named.OriginalDefinition.MetadataName != $"{TypeLibraryGenerator.ZodRefineContextName}`1"
			|| named.ContainingNamespace.ToDisplayString() != TypeLibraryGenerator.ZodSharpSchemasNamespace
			|| named.TypeArguments.Length != 1
		)
		{
			return false;
		}

		// The RefineCtx<T> type argument must be the same as the containing type, but the containing type may be
		return SymbolEqualityComparer.Default.Equals(named.TypeArguments[0], classSymbol);
	}

	static bool ReturnsEnumerableOfValidationError(IMethodSymbol method)
	{
		var returnType = method.ReturnType;
		if (returnType is IArrayTypeSymbol arrayType)
			return IsValidationErrorType(arrayType.ElementType);

		if (
			returnType is INamedTypeSymbol { OriginalDefinition.MetadataName: "IEnumerable`1" } named
			&& named.TypeArguments.Length == 1
		)
		{
			return IsValidationErrorType(named.TypeArguments[0]);
		}

		foreach (var iface in returnType.AllInterfaces)
		{
			if (
				iface is INamedTypeSymbol { OriginalDefinition.MetadataName: "IEnumerable`1" } enumerable
				&& enumerable.TypeArguments.Length == 1
				&& IsValidationErrorType(enumerable.TypeArguments[0])
			)
			{
				return true;
			}
		}

		return false;
	}

	static bool IsValidationErrorType(ITypeSymbol type) =>
		type is INamedTypeSymbol { MetadataName: "ValidationError" } named
		&& named.ContainingNamespace.ToDisplayString() == TypeLibraryGenerator.ZodSharpCoreNamespace;
}
