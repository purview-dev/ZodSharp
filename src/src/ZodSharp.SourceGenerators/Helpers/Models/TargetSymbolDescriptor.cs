using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZodSharp.SourceGenerators.Helpers.Models;

sealed record class TargetSymbolDescriptor(INamedTypeSymbol Symbol, TypeDeclarationSyntax Declaration);
