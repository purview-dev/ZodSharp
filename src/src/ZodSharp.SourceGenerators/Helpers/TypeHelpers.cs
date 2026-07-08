using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers;

static class TypeHelpers
{
	public const string DisableZodSharpSourceGeneratorProperty = "build_property.DisableZodSharpSourceGenerator";

	// This matches the name of the class, just so we can use the `nameof` for later...
	public const string ZodSchemaAttribute = "ZodSharp.ZodSchemaAttribute";

	// Data Annotations attributes metadata names
	public const string RequiredAttributeMetadataName = "System.ComponentModel.DataAnnotations.RequiredAttribute";
	public const string EmailAddressAttributeMetadataName =
		"System.ComponentModel.DataAnnotations.EmailAddressAttribute";
	public const string StringLengthAttributeMetadataName =
		"System.ComponentModel.DataAnnotations.StringLengthAttribute";
	public const string MinLengthAttributeMetadataName = "System.ComponentModel.DataAnnotations.MinLengthAttribute";
	public const string MaxLengthAttributeMetadataName = "System.ComponentModel.DataAnnotations.MaxLengthAttribute";
	public const string RangeAttributeMetadataName = "System.ComponentModel.DataAnnotations.RangeAttribute";
	public const string LengthAttributeMetadataName = "System.ComponentModel.DataAnnotations.LengthAttribute";
	public const string RegularExpressionAttributeMetadataName =
		"System.ComponentModel.DataAnnotations.RegularExpressionAttribute";
	public const string AllowedValuesAttributeMetadataName =
		"System.ComponentModel.DataAnnotations.AllowedValuesAttribute";
	public const string DeniedValuesAttributeMetadataName =
		"System.ComponentModel.DataAnnotations.DeniedValuesAttribute";

	// Other ZodSharp types...
	public const string ValidationResult = "ZodSharp.Core.ValidationResult";
	public const string ValidationError = "ZodSharp.Core.ValidationError";

	// System Types
	public static readonly string ImmutableArrayMetadataName = typeof(ImmutableArray).FullName;

	public static string GetLimitedAccessibilityKeyword(INamedTypeSymbol symbol)
	{
		return symbol.DeclaredAccessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Private => "private",
			_ => "internal",
		};
	}

	public static string GetAccessibilityKeyword(INamedTypeSymbol symbol)
	{
		return symbol.DeclaredAccessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Internal => "internal",
			Accessibility.Private => "private",
			Accessibility.Protected => "protected",
			Accessibility.ProtectedAndInternal => "private protected",
			Accessibility.ProtectedOrInternal => "protected internal",
			//Accessibility.File => "file",
			_ => string.Empty,
		};
	}

	public static bool CanBeNull(ITypeSymbol typeSymbol) =>
		typeSymbol.IsReferenceType
		|| typeSymbol.NullableAnnotation == NullableAnnotation.Annotated
		|| typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };

	public static bool IsNumericType(ITypeSymbol type) =>
		type.SpecialType
			is SpecialType.System_Byte
				or SpecialType.System_SByte
				or SpecialType.System_Int16
				or SpecialType.System_UInt16
				or SpecialType.System_Int32
				or SpecialType.System_UInt32
				or SpecialType.System_Int64
				or SpecialType.System_UInt64
				or SpecialType.System_Single
				or SpecialType.System_Double
				or SpecialType.System_Decimal;
}
