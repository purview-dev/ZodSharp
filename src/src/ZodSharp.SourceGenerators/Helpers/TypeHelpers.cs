using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Helpers;

static class TypeHelpers
{
	public const string ZodSchemaAttributeMetadataName = "ZodSharp.ZodSchemaAttribute";

	// Data Annotations attributes metadata names
	public const string RequiredAttributeMetadataName = "System.ComponentModel.DataAnnotations.RequiredAttribute";
	public const string EmailAddressAttributeMetadataName =
		"System.ComponentModel.DataAnnotations.EmailAddressAttribute";
	public const string StringLengthAttributeMetadataName =
		"System.ComponentModel.DataAnnotations.StringLengthAttribute";
	public const string MinLengthAttributeMetadataName = "System.ComponentModel.DataAnnotations.MinLengthAttribute";
	public const string MaxLengthAttributeMetadataName = "System.ComponentModel.DataAnnotations.MaxLengthAttribute";
	public const string RangeAttributeMetadataName = "System.ComponentModel.DataAnnotations.RangeAttribute";

	// Other ZodSharp types...
	public const string ValidationResult = "ZodSharp.Core.ValidationResult";
	public const string ValidationError = "ZodSharp.Core.ValidationError";

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
