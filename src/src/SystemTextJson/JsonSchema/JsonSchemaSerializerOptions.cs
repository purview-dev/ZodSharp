using System.Text.Json;

namespace ZodSharp.JsonSchema;

/// <summary>
/// Options for JSON serialization/deserialization of JSON Schema using System.Text.Json.
/// </summary>
public static class JsonSchemaSerializerOptions
{
	/// <summary>
	/// Default settings for JSON Schema serialization.
	/// Uses camelCase property naming and ignores null values.
	/// </summary>
	public static readonly JsonSerializerOptions Default = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = true,
	};

	/// <summary>
	/// Settings for reading JSON Schema with flexible property matching.
	/// </summary>
	public static readonly JsonSerializerOptions Reading = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
	};
}
