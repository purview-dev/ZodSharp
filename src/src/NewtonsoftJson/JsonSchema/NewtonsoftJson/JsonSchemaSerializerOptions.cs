using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ZodSharp.JsonSchema.NewtonsoftJson;

/// <summary>
/// Resolves the JSON Schema keyword property names (<c>$schema</c>, <c>$id</c>, <c>$ref</c> and
/// <c>$defs</c>) while keeping camelCase naming for every other member.
/// </summary>
sealed class JsonSchemaKeywordContractResolver : Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver
{
	static readonly Dictionary<string, string> KeywordNames = new(StringComparer.Ordinal)
	{
		["Schema"] = "$schema",
		["Id"] = "$id",
		["Ref"] = "$ref",
		["Defs"] = "$defs",
	};

	protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
	{
		var properties = base.CreateProperties(type, memberSerialization);

		if (type != typeof(ZodSharp.JsonSchema.JsonSchemaDefinition))
		{
			return properties;
		}

		foreach (var property in properties)
		{
			if (
				property.UnderlyingName != null
				&& KeywordNames.TryGetValue(property.UnderlyingName, out var keywordName)
			)
			{
				property.PropertyName = keywordName;
			}
		}

		return properties;
	}
}

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
		MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
		ContractResolver = new JsonSchemaKeywordContractResolver(),
	};

	/// <summary>
	/// Settings for reading JSON Schema with flexible property matching.
	/// </summary>
	public static readonly JsonSerializerSettings Reading = new()
	{
		NullValueHandling = NullValueHandling.Ignore,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
		ContractResolver = new JsonSchemaKeywordContractResolver(),
	};
}
