using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZodSharp.CodeFixes;

/// <summary>
/// Code fix for <c>ZODSGEN041</c>. Rewrites <c>Z.Union&lt;T1, T2&gt;(...)</c> to
/// <c>Z.NativeUnion&lt;T1, T2&gt;(...)</c> and <c>new ZodTypedUnion&lt;T1, T2&gt;(...)</c> to
/// <c>new ZodTypedNativeUnion&lt;T1, T2&gt;(...)</c>, retargeting an explicit
/// <c>ZodTypedUnion&lt;...&gt;</c> declaration so the fixed code still compiles.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NativeUnionCodeFixProvider))]
[Shared]
public sealed class NativeUnionCodeFixProvider : CodeFixProvider
{
	/// <summary>The diagnostic id reported by the ZodSharp analyzer.</summary>
	const string DiagnosticId = "ZODSGEN041";

	const string Title = "Use native C# 15 union";

	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticId];

	/// <inheritdoc/>
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		var diagnostic = context.Diagnostics[0];
		var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

		var replacement = node switch
		{
			InvocationExpressionSyntax invocation when IsUnionMethod(invocation) => "NativeUnion",
			ObjectCreationExpressionSyntax creation when IsTypedUnionType(creation.Type) => "ZodTypedNativeUnion",
			_ => null,
		};

		if (replacement is null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Title,
				cancellationToken => RewriteAsync(context.Document, node, replacement, cancellationToken),
				equivalenceKey: Title
			),
			diagnostic
		);
	}

	static bool IsUnionMethod(InvocationExpressionSyntax invocation) =>
		invocation.Expression
			is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Union" }
				or IdentifierNameSyntax { Identifier.ValueText: "Union" };

	static bool IsTypedUnionType(TypeSyntax? type) =>
		type
			is GenericNameSyntax { Identifier.ValueText: "ZodTypedUnion" }
				or IdentifierNameSyntax { Identifier.ValueText: "ZodTypedUnion" };

	static async Task<Document> RewriteAsync(
		Document document,
		SyntaxNode target,
		string replacement,
		CancellationToken cancellationToken
	)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		var renamed = target switch
		{
			InvocationExpressionSyntax invocation => invocation.WithExpression(
				RenameExpression(invocation.Expression, replacement)
			),
			ObjectCreationExpressionSyntax creation => creation.WithType(RenameType(creation.Type, replacement)),
			_ => target,
		};

		var newRoot = root.ReplaceNode(target, renamed);

		// An explicit `ZodTypedUnion<...>` declaration must also move to `ZodTypedNativeUnion<...>`,
		// otherwise the fixed assignment no longer compiles. The declared type precedes the
		// invocation/creation, so its span is unaffected by the rename above.
		var declarationType = FindDeclarationType(target);
		if (declarationType is not null)
		{
			var declarationTypeInNewRoot =
				newRoot.FindNode(declarationType.Span, getInnermostNodeForTie: true) as TypeSyntax;

			if (declarationTypeInNewRoot is not null && IsTypedUnionType(declarationTypeInNewRoot))
			{
				newRoot = newRoot.ReplaceNode(
					declarationTypeInNewRoot,
					RenameType(declarationTypeInNewRoot, "ZodTypedNativeUnion")
				);
			}
		}

		return document.WithSyntaxRoot(newRoot);
	}

	static TypeSyntax? FindDeclarationType(SyntaxNode target)
	{
		if (
			target.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault() is { } declaration
			&& IsTypedUnionType(declaration.Type)
		)
		{
			return declaration.Type;
		}

		if (
			target.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault() is { } property
			&& IsTypedUnionType(property.Type)
		)
		{
			return property.Type;
		}

		// Handle cases where the target is part of a field declaration
		return null;
	}

	static ExpressionSyntax RenameExpression(ExpressionSyntax expression, string replacement) =>
		expression switch
		{
			MemberAccessExpressionSyntax member => member.WithName(RenameSimpleName(member.Name, replacement)),
			SimpleNameSyntax simple => RenameSimpleName(simple, replacement),
			_ => expression,
		};

	static SimpleNameSyntax RenameSimpleName(SimpleNameSyntax name, string replacement) =>
		name switch
		{
			GenericNameSyntax generic => generic.WithIdentifier(SyntaxFactory.Identifier(replacement)),
			_ => SyntaxFactory.IdentifierName(replacement),
		};

	static TypeSyntax RenameType(TypeSyntax type, string replacement) =>
		type switch
		{
			GenericNameSyntax generic => generic.WithIdentifier(SyntaxFactory.Identifier(replacement)),
			_ => SyntaxFactory.IdentifierName(replacement),
		};
}
