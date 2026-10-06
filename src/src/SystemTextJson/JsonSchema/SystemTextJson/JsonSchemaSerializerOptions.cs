using System.Text.Json;

namespace ZodSharp.JsonSchema.SystemTextJson;

// The custom JsonSchemaNamingPolicy that used to live here has been removed. A naming policy is a runtime
// object, so the System.Text.Json source generator cannot reproduce it, and JSON Schema (de)serialization
// could not use a source-generated — trim- and AOT-safe — contract while it existed. The four $-prefixed
// keyword names are now declared with [JsonPropertyName] on JsonSchemaDefinition, which both the generated
// and reflection contracts honour identically, and the rest is the built-in camelCase policy.

/// <summary>
/// Options for JSON serialization/deserialization of JSON Schema using System.Text.Json.
/// </summary>
public static class JsonSchemaSerializerOptions
{
	/// <summary>
	/// Default settings for JSON Schema serialization.
	/// Writes the JSON Schema keyword names (<c>$schema</c>, <c>$id</c>, <c>$ref</c>, <c>$defs</c>) and
	/// camelCase for every other member, and ignores null values.
	/// </summary>
	/// <remarks>
	/// Resolves contracts through the source-generated <see cref="JsonSchemaJsonContext"/>, so JSON Schema
	/// export is safe under trimming and Native AOT. The <c>$</c>-prefixed keyword names now come from
	/// <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/> on
	/// <see cref="JsonSchemaDefinition"/> rather than from a runtime naming policy, which the source
	/// generator cannot reproduce; the wire format is unchanged.
	/// </remarks>
	public static readonly JsonSerializerOptions Default = new()
	{
		TypeInfoResolver = JsonSchemaJsonContext.Default,
		// The built-in camelCase policy, not a custom one: a JsonNamingPolicy set in
		// JsonSourceGenerationOptions is not carried onto a separate JsonSerializerOptions instance that
		// merely uses the context as its resolver, so it has to be stated here too. [JsonPropertyName] on
		// JsonSchemaDefinition still wins, which is what keeps the $-prefixed keyword names.
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = true,
	};

	/// <summary>
	/// Settings for reading JSON Schema with flexible property matching.
	/// </summary>
	/// <remarks>See <see cref="Default"/>.</remarks>
	public static readonly JsonSerializerOptions Reading = new()
	{
		TypeInfoResolver = JsonSchemaJsonContext.Default,
		// The built-in camelCase policy, not a custom one: a JsonNamingPolicy set in
		// JsonSourceGenerationOptions is not carried onto a separate JsonSerializerOptions instance that
		// merely uses the context as its resolver, so it has to be stated here too. [JsonPropertyName] on
		// JsonSchemaDefinition still wins, which is what keeps the $-prefixed keyword names.
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
	};
}
