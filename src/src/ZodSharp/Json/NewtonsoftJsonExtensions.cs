using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZodSharp.Core;

namespace ZodSharp.Json;

/// <summary>
/// Extensions for integrating ZodSharp with Newtonsoft.Json.
/// </summary>
public static class NewtonsoftJsonExtensions
{
	static readonly string[] EmptyPath = Array.Empty<string>();

	/// <summary>
	/// Deserializes JSON and validates it using a Zod schema.
	/// </summary>
	public static ValidationResult<T> DeserializeAndValidate<T>(
		this IZodSchema<T, T> schema,
		string json,
		JsonSerializerSettings? settings = null
	)
	{
		try
		{
			var deserialized = JsonConvert.DeserializeObject<T>(json, settings);
			if (deserialized == null)
			{
				return ValidationResult<T>.Failure(
					new ValidationError("deserialization_failed", "Failed to deserialize JSON", EmptyPath)
				);
			}

			return schema.Validate(deserialized);
		}
		catch (JsonException ex)
		{
			return ValidationResult<T>.Failure(
				new ValidationError("json_error", $"JSON parsing error: {ex.Message}", EmptyPath)
			);
		}
	}

	/// <summary>
	/// Deserializes JSON and validates it using a Zod schema (async).
	/// </summary>
	public static async Task<ValidationResult<T>> DeserializeAndValidateAsync<T>(
		this IZodSchema<T, T> schema,
		Stream jsonStream,
		JsonSerializerSettings? settings = null
	)
	{
		try
		{
#if NETSTANDARD2_1
			using (var reader = new StreamReader(jsonStream, Encoding.UTF8, true, 1024, true))
			{
				var json = await reader.ReadToEndAsync();
				return schema.DeserializeAndValidate<T>(json, settings);
			}
#else
			using var reader = new StreamReader(jsonStream, Encoding.UTF8, leaveOpen: true);
			var json = await reader.ReadToEndAsync();
			return schema.DeserializeAndValidate<T>(json, settings);
#endif
		}
		catch (JsonException ex)
		{
			return ValidationResult<T>.Failure(
				new ValidationError("json_error", $"JSON parsing error: {ex.Message}", EmptyPath)
			);
		}
	}

	/// <summary>
	/// Deserializes JSON from a JToken and validates it using a Zod schema.
	/// </summary>
	public static ValidationResult<T> DeserializeAndValidate<T>(
		this IZodSchema<T, T> schema,
		JToken token,
		JsonSerializer? serializer = null
	)
	{
		try
		{
			var deserialized = token.ToObject<T>(serializer ?? JsonSerializer.CreateDefault());
			if (deserialized == null)
			{
				return ValidationResult<T>.Failure(
					new ValidationError("deserialization_failed", "Failed to deserialize JSON", EmptyPath)
				);
			}

			return schema.Validate(deserialized);
		}
		catch (JsonException ex)
		{
			return ValidationResult<T>.Failure(
				new ValidationError("json_error", $"JSON parsing error: {ex.Message}", EmptyPath)
			);
		}
	}

	/// <summary>
	/// Creates a custom JsonConverter that validates using a Zod schema.
	/// </summary>
	public static JsonConverter CreateValidatingConverter<T>(IZodSchema<T, T> schema)
	{
		return new ZodJsonConverter<T>(schema);
	}
}

/// <summary>
/// JsonConverter that validates using a Zod schema.
/// </summary>
class ZodJsonConverter<T> : JsonConverter<T>
{
	readonly IZodSchema<T, T> _schema;

	public ZodJsonConverter(IZodSchema<T, T> schema)
	{
		_schema = schema;
	}

	public override T ReadJson(
		JsonReader reader,
		Type objectType,
		T? existingValue,
		bool hasExistingValue,
		JsonSerializer serializer
	)
	{
		var token = JToken.Load(reader);
		var deserialized = token.ToObject<T>(serializer);

		if (deserialized == null)
		{
			throw new JsonSerializationException("Failed to deserialize JSON");
		}

		var result = _schema.Validate(deserialized);
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

		var result = _schema.Validate(value);
		if (!result.IsSuccess)
		{
			var errorMessages = string.Join(", ", result.Errors.Select(e => e.Message));
			throw new JsonSerializationException($"Validation failed: {errorMessages}");
		}

		var token = JToken.FromObject(value, serializer);
		token.WriteTo(writer);
	}
}
