namespace ZodSharp.SourceGenerators.Helpers;

static partial class TypeLibrary
{
	public const string ZodSharpNamespace = "ZodSharp";

	public const string ZodSharpCoreNamespace = ZodSharpNamespace + ".Core";

	// Default custom async validation method name when none is explicitly configured.
	public const string DefaultCustomValidationMethodName = "CustomValidationAsync";

	// This matches the name of the class, just so we can use the `nameof` for later...
	public static readonly TypeValueObject ZodSchemaAttribute = new(nameof(ZodSchemaAttribute), ZodSharpNamespace);

	public static readonly TypeValueObject ZodSchemaGeneratedAttribute = new(
		nameof(ZodSchemaGeneratedAttribute),
		ZodSharpCoreNamespace
	);

	// Other ZodSharp types...
	public static readonly TypeValueObject ValidationResult = new(nameof(ValidationResult), ZodSharpCoreNamespace);

	public static readonly TypeValueObject ValidationResultMetadataName = new(
		nameof(ValidationResultMetadataName),
		ZodSharpCoreNamespace
	);

	public static readonly TypeValueObject ValidationError = new(nameof(ValidationError), ZodSharpCoreNamespace);

	public static class System
	{
		public static readonly TypeValueObject String = new(Microsoft.CodeAnalysis.SpecialType.System_String);

		public static readonly TypeValueObject Boolean = new(Microsoft.CodeAnalysis.SpecialType.System_Boolean);

		public static readonly TypeValueObject AttributeUsageAttribute = new(typeof(AttributeUsageAttribute));

		public static readonly TypeValueObject AttributeTargets = new(typeof(AttributeTargets));
	}
}
