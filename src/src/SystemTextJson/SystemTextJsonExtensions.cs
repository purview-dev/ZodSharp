using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZodSharp.Core;
using ZodSharp.Json;

namespace ZodSharp;

/// <summary>
/// Extensions for integrating ZodSharp with System.Text.Json.
/// </summary>
#if !NETSTANDARD2_1_OR_GREATER
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class SystemTextJsonExtensions
{
	static readonly string[] EmptyPath = [];

	/// <summary>
	/// Deserializes JSON and validates it using a Zod schema.
	/// </summary>
	public static ValidationResult<T> DeserializeAndValidate<T>(
		this IZodSchema<T, T> schema,
		string json,
		JsonSerializerOptions? options = null
	)
	{
		if (schema == null)
			throw new ArgumentNullException(nameof(schema));
		if (json == null)
			throw new ArgumentNullException(nameof(json));

		try
		{
			var deserialized = JsonSerializer.Deserialize<T>(json, options);
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
	/// Deserializes JSON from a stream and validates it using a Zod schema (async).
	/// </summary>
	public static async ValueTask<ValidationResult<T>> DeserializeAndValidateAsync<T>(
		this IZodSchema<T, T> schema,
		Stream jsonStream,
		JsonSerializerOptions? options = null,
		CancellationToken cancellationToken = default
	)
	{
		if (schema == null)
			throw new ArgumentNullException(nameof(schema));
		if (jsonStream == null)
			throw new ArgumentNullException(nameof(jsonStream));

		try
		{
			T? deserialized;
#if NETSTANDARD2_1_OR_GREATER
			// System.Text.Json on netstandard2.1 lacks the CancellationToken overload of DeserializeAsync.
			using StreamReader reader = new(jsonStream, Encoding.UTF8, true, 1024, true);
			var json = await reader.ReadToEndAsync();
			deserialized = JsonSerializer.Deserialize<T>(json, options);
#else
			deserialized = await JsonSerializer.DeserializeAsync<T>(jsonStream, options, cancellationToken);
#endif
			return deserialized == null
				? ValidationResult<T>.Failure(
					new ValidationError("deserialization_failed", "Failed to deserialize JSON", EmptyPath)
				)
				: await schema.ValidateAsync(deserialized);
		}
		catch (JsonException ex)
		{
			return ValidationResult<T>.Failure(
				new ValidationError("json_error", $"JSON parsing error: {ex.Message}", EmptyPath)
			);
		}
	}

	/// <summary>
	/// Validates a value and serializes it to a JSON string.
	/// </summary>
	public static ValidationResult<string> ValidateAndSerialize<T>(
		this IZodSchema<T, T> schema,
		T value,
		JsonSerializerOptions? options = null
	)
	{
		if (schema == null)
			throw new ArgumentNullException(nameof(schema));

		var result = schema.Validate(value);
		if (!result.IsSuccess)
			return ValidationResult<string>.Failure(result.Errors);

		var json = JsonSerializer.Serialize(result.Value, options);
		return ValidationResult<string>.Success(json);
	}

	/// <summary>
	/// Validates a value and serializes it to a stream (async).
	/// </summary>
	public static async ValueTask<ValidationResult<string>> ValidateAndSerializeAsync<T>(
		this IZodSchema<T, T> schema,
		T value,
		Stream output,
		JsonSerializerOptions? options = null,
		CancellationToken cancellationToken = default
	)
	{
		if (schema == null)
			throw new ArgumentNullException(nameof(schema));
		if (output == null)
			throw new ArgumentNullException(nameof(output));

		var result = await schema.ValidateAsync(value);
		if (!result.IsSuccess)
			return ValidationResult<string>.Failure(result.Errors);

		await JsonSerializer.SerializeAsync(output, result.Value, options, cancellationToken);
		return ValidationResult<string>.Success(string.Empty);
	}

	/// <summary>
	/// Creates a custom JsonConverter that validates using a Zod schema.
	/// </summary>
	public static JsonConverter<T> CreateValidatingConverter<T>(this IZodSchema<T, T> schema) =>
		schema == null ? throw new ArgumentNullException(nameof(schema)) : new ZodJsonConverter<T>(schema);
}
