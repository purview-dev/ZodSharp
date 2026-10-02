using System.Text.Json;

namespace ZodSharp.JsonSchema.SystemTextJson;

/// <summary>
/// Names the JSON Schema keyword properties with their specification names (<c>$schema</c>, <c>$id</c>,
/// <c>$ref</c> and <c>$defs</c>) and camelCase for every other member.
/// </summary>
sealed class JsonSchemaNamingPolicy : JsonNamingPolicy
{
	static readonly Dictionary<string, string> KeywordNames = new(StringComparer.Ordinal)
	{
		["Schema"] = "$schema",
		["Id"] = "$id",
		["Ref"] = "$ref",
		["Defs"] = "$defs",
	};

	public override string ConvertName(string name) =>
		KeywordNames.TryGetValue(name, out var keywordName) ? keywordName : CamelCase.ConvertName(name);
}

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
	public static readonly JsonSerializerOptions Default = new()
	{
		PropertyNamingPolicy = new JsonSchemaNamingPolicy(),
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = true,
	};

	/// <summary>
	/// Settings for reading JSON Schema with flexible property matching.
	/// </summary>
	public static readonly JsonSerializerOptions Reading = new()
	{
		PropertyNamingPolicy = new JsonSchemaNamingPolicy(),
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
	};
}
