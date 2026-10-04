namespace ZodSharp.JsonSchema;

/// <summary>
/// Options for converting ZodSharp schemas to JSON Schema.
/// </summary>
public sealed class ToJsonSchemaOptions
{
	/// <summary>
	/// Whether to include the $schema property in the output.
	/// Default: true
	/// </summary>
	public bool IncludeSchema { get; set; } = true;

	/// <summary>
	/// Custom $id for the schema.
	/// </summary>
	public string? Id { get; set; }

	/// <summary>
	/// Custom title for the schema.
	/// </summary>
	public string? Title { get; set; }
}
