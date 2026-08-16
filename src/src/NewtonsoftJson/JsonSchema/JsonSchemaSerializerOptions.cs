using Newtonsoft.Json;

namespace ZodSharp.JsonSchema;

/// <summary>
/// Options for JSON serialization/deserialization of JSON Schema.
/// </summary>
public static class JsonSchemaSerializerOptions
{
	/// <summary>
	/// Default settings for JSON Schema serialization.
	/// Uses camelCase property naming and ignores null values.
	/// </summary>
	public static readonly JsonSerializerSettings Default = new()
	{
		NullValueHandling = NullValueHandling.Ignore,
		Formatting = Formatting.Indented,
		ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
	};

	/// <summary>
	/// Settings for reading JSON Schema with flexible property matching.
	/// </summary>
	public static readonly JsonSerializerSettings Reading = new()
	{
		NullValueHandling = NullValueHandling.Ignore,
		MissingMemberHandling = MissingMemberHandling.Ignore,
	};
}
