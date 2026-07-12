using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZodSharp.Core;
using ZodSharp.Json;

namespace ZodSharp;

/// <summary>
/// Extensions for integrating ZodSharp with Newtonsoft.Json.
/// </summary>
#if !NETSTANDARD2_1_OR_GREATER
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class NewtonsoftJsonExtensions
{
	static readonly string[] EmptyPath = [];

	/// <summary>
	/// Deserializes JSON and validates it using a Zod schema.
	/// </summary>
	public static ValidationResult<T> DeserializeAndValidate<T>(
		this IZodSchema<T, T> schema,
		string json,
		JsonSerializerSettings? settings = null
	)
	{
		if (schema == null)
			throw new ArgumentNullException(nameof(schema));

		try
		{
			var deserialized = JsonConvert.DeserializeObject<T>(json, settings);
			return deserialized == null
				? ValidationResult<T>.Failure(
					new ValidationError("deserialization_failed", "Failed to deserialize JSON", EmptyPath)
				)
				: schema.Validate(deserialized);
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
		JsonSerializerSettings? settings = null,
		CancellationToken cancellationToken = default
	)
	{
		try
		{
#if NETSTANDARD2_1_OR_GREATER
			using StreamReader reader = new(jsonStream, Encoding.UTF8, true, 1024, true);
			var json = await reader.ReadToEndAsync();
#else
			using StreamReader reader = new(jsonStream, Encoding.UTF8, leaveOpen: true);
			var json = await reader.ReadToEndAsync(cancellationToken);
#endif

			return schema.DeserializeAndValidate<T>(json, settings);
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
		if (schema == null)
			throw new ArgumentNullException(nameof(schema));
		if (token == null)
			throw new ArgumentNullException(nameof(token));

		try
		{
			var deserialized = token.ToObject<T>(serializer ?? JsonSerializer.CreateDefault());
			return deserialized == null
				? ValidationResult<T>.Failure(
					new ValidationError("deserialization_failed", "Failed to deserialize JSON", EmptyPath)
				)
				: schema.Validate(deserialized);
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
	public static JsonConverter CreateValidatingConverter<T>(this IZodSchema<T, T> schema) =>
		schema == null ? throw new ArgumentNullException(nameof(schema)) : new ZodJsonConverter<T>(schema);
}
