namespace ZodSharp;

/// <summary>
/// Attribute to mark a class for automatic schema generation.
/// When applied to a class, a zero-allocation validator will be generated at compile time.
/// </summary>
{{CodeGen}}
[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
sealed class ZodSchemaAttribute : global::System.Attribute
{
	/// <summary>
	/// Optional name for the generated schema class.
	/// If not specified, uses "{ClassName}Schema".
	/// </summary>
	public string? SchemaName { get; set; }

	/// <summary>
	/// Whether to generate a static Validate method.
	/// Default is true.
	/// </summary>
	public bool GenerateValidateMethod { get; set; } = true;

	/// <summary>
	/// Whether to generate a static Parse method (throws on failure).
	/// Default is true.
	/// </summary>
	public bool GenerateParseMethod { get; set; } = true;

	/// <summary>
	/// Whether to enable composition methods (.and(), .or(), .refine()).
	/// Default is true.
	/// </summary>
	public bool EnableComposition { get; set; } = true;
}
