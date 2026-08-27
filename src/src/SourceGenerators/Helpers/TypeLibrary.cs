namespace ZodSharp.SourceGenerators.Helpers;

static partial class TypeLibrary
{
	public const string ZodSharpNamespace = "ZodSharp";

	public const string ZodSharpCoreNamespace = ZodSharpNamespace + ".Core";

	// Default custom async validation method name when none is explicitly configured.
	public const string DefaultCustomValidationMethodName = "CustomValidationAsync";

	// This matches the name of the class, just so we can use the `nameof` for later...
	public static readonly TypeIdentity ZodSchemaAttribute = new(nameof(ZodSchemaAttribute), ZodSharpNamespace);

	public static readonly TypeIdentity ZodSchemaGeneratedAttribute = new(
		nameof(ZodSchemaGeneratedAttribute),
		ZodSharpCoreNamespace
	);

	// Other ZodSharp types...
	public static readonly TypeIdentity ValidationResult = new(nameof(ValidationResult), ZodSharpCoreNamespace);

	public static readonly TypeIdentity ValidationResultMetadataName = new(
		nameof(ValidationResultMetadataName),
		ZodSharpCoreNamespace
	);

	public static readonly TypeIdentity ValidationError = new(nameof(ValidationError), ZodSharpCoreNamespace);
}
