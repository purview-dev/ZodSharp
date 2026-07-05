using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZodSharp.Core;

namespace ZodSharp.Json;

/// <summary>
/// JsonConverter that validates using a Zod schema.
/// </summary>
sealed class ZodJsonConverter<T>(IZodSchema<T, T> schema) : JsonConverter<T>
{
	public override T ReadJson(
		JsonReader reader,
		Type objectType,
		T? existingValue,
		bool hasExistingValue,
		JsonSerializer serializer
	)
	{
		var token = JToken.Load(reader);
		var deserialized =
			token.ToObject<T>(serializer) ?? throw new JsonSerializationException("Failed to deserialize JSON");

		var result = schema.Validate(deserialized);
		if (!result.IsSuccess)
		{
			var errorMessages = string.Join(", ", result.Errors.Select(e => e.Message));
			throw new JsonSerializationException($"Validation failed: {errorMessages}");
		}

		return result.Value!;
	}

	public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}

		var result = schema.Validate(value);
		if (!result.IsSuccess)
		{
			var errorMessages = string.Join(", ", result.Errors.Select(e => e.Message));
			throw new JsonSerializationException($"Validation failed: {errorMessages}");
		}

		var token = JToken.FromObject(value, serializer);
		token.WriteTo(writer);
	}
}
