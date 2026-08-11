using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Models;

namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Provides helpers for type analysis and identifier generation during source generation.
/// </summary>
public static class TypeHelpers
{
	/// <summary>
	/// The suffix used to identify attribute types.
	/// </summary>
	public const string AttributeSuffix = nameof(Attribute);

	static readonly (string Keyword, SpecialType SpecialType)[] Map =
	[
		("bool", SpecialType.System_Boolean),
		("byte", SpecialType.System_Byte),
		("sbyte", SpecialType.System_SByte),
		("char", SpecialType.System_Char),
		("decimal", SpecialType.System_Decimal),
		("double", SpecialType.System_Double),
		("float", SpecialType.System_Single),
		("int", SpecialType.System_Int32),
		("uint", SpecialType.System_UInt32),
		("long", SpecialType.System_Int64),
		("ulong", SpecialType.System_UInt64),
		("short", SpecialType.System_Int16),
		("ushort", SpecialType.System_UInt16),
		("string", SpecialType.System_String),
		("object", SpecialType.System_Object),
		("void", SpecialType.System_Void),
		("nint", SpecialType.System_IntPtr),
		("nuint", SpecialType.System_UIntPtr),
	];

	static readonly ImmutableDictionary<string, SpecialType> KeywordToSpecialType =
		Map.ToImmutableDictionary(m => m.Keyword, m => m.SpecialType, StringComparer.Ordinal);

	static readonly ImmutableDictionary<SpecialType, string> SpecialTypeToKeyword =
		Map.ToImmutableDictionary(m => m.SpecialType, m => m.Keyword);

	/// <summary>
	/// Tries to map a C# keyword to its corresponding <see cref="SpecialType"/>.
	/// </summary>
	public static bool TryGetSpecialType(string keyword, out SpecialType specialType) =>
		KeywordToSpecialType.TryGetValue(keyword, out specialType);

	/// <summary>
	/// Tries to map a <see cref="SpecialType"/> to its corresponding C# keyword.
	/// </summary>
	public static bool TryGetKeyword(SpecialType specialType, out string? keyword) =>
		SpecialTypeToKeyword.TryGetValue(specialType, out keyword);

	/// <summary>
	/// Determines whether the specified type is a C# keyword type.
	/// </summary>
	public static bool IsKeywordType(ITypeSymbol type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		return SpecialTypeToKeyword.ContainsKey(type.SpecialType);
	}

	/// <summary>
	/// Determines whether the specified keyword is a recognized C# keyword type.
	/// </summary>
	public static bool IsKeywordType(string keyword) => KeywordToSpecialType.ContainsKey(keyword);

	/// <summary>
	/// Determines whether the supplied type name ends with the 'Attribute' suffix.
	/// </summary>
	public static bool IsAttribute(string typeName)
	{
		if (typeName == null)
			throw new ArgumentNullException(nameof(typeName));

		var idx = typeName.IndexOf('`');
		if (idx >= 0)
			typeName = typeName.Substring(0, idx);

		return typeName.Length > AttributeSuffix.Length
			&& typeName.EndsWith(AttributeSuffix, StringComparison.Ordinal);
	}

	/// <summary>
	/// Gets the type name without the 'Attribute' suffix, if present.
	/// </summary>
	public static string GetTypeName(string typeName)
	{
		if (typeName == null)
			throw new ArgumentNullException(nameof(typeName));

		var idx = typeName.IndexOf('`');
		if (idx >= 0)
			typeName = typeName.Substring(0, idx);

		if (IsAttribute(typeName))
			typeName = typeName.Substring(0, typeName.Length - AttributeSuffix.Length);

		return typeName;
	}

	/// <summary>
	/// Determines whether the target symbol has an explicit base type declaration.
	/// </summary>
	public static bool HasExplicitBaseType(TargetSymbolDescriptor descriptor)
	{
		if (descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));
		if (descriptor.Declaration == null)
			return false;

		return descriptor.Declaration.BaseList is { Types.Count: > 0 };
	}

	/// <summary>
	/// Determines whether the target symbol is derived from the expected base type.
	/// </summary>
	public static bool IsDerivedFromExpectedBase(
		TargetSymbolDescriptor descriptor,
		TypeValueObject expectedBase
	)
	{
		if (descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));
		if (descriptor.Symbol.BaseType is not null)
		{
			TypeValueObject baseType = new(descriptor.Symbol.BaseType);
			if (baseType == expectedBase)
				return true;
		}

		var declaredBaseTypes = descriptor.Declaration?.BaseList?.Types;
		if (declaredBaseTypes is null)
			return false;

		foreach (var baseType in declaredBaseTypes)
		{
			if (
				string.Equals(
					GetUnqualifiedTypeName(baseType.Type),
					expectedBase.SymbolFullName,
					StringComparison.Ordinal
				)
			)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the unqualified type name from a <see cref="TypeSyntax"/>.
	/// </summary>
	public static string GetUnqualifiedTypeName(TypeSyntax typeSyntax) =>
		typeSyntax switch
		{
			IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
			GenericNameSyntax genericName => genericName.Identifier.ValueText,
			QualifiedNameSyntax qualifiedName => GetUnqualifiedTypeName(qualifiedName.Right),
			AliasQualifiedNameSyntax aliasQualifiedName => GetUnqualifiedTypeName(
				aliasQualifiedName.Name
			),
			NullableTypeSyntax nullableType => GetUnqualifiedTypeName(nullableType.ElementType),
			_ => typeSyntax.ToString(),
		};

	/// <summary>
	/// Determines whether the supplied name is a valid C# identifier.
	/// </summary>
	public static bool IsValidIdentifier(string? name)
	{
		if (string.IsNullOrEmpty(name))
			return false;
		if (!char.IsLetter(name![0]) && name[0] != '_')
			return false;
		for (var i = 1; i < name.Length; i++)
		{
			if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
				return false;
		}

		return true;
	}

	/// <summary>
	/// Creates a <see cref="TypeValueObject"/> for the embedded compiler attribute used by source generators.
	/// </summary>
	public static readonly TypeValueObject EmbeddedAttribute = new(
		nameof(EmbeddedAttribute),
		"Microsoft.CodeAnalysis"
	);
}
