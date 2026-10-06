using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ZodSharp.Core;
using ZodSharp.Json;
#if NETSTANDARD2_1_OR_GREATER
using System.Text;
#endif

namespace ZodSharp;

/// <summary>
/// Extensions for integrating ZodSharp with System.Text.Json.
/// </summary>
#if !NETSTANDARD2_1_OR_GREATER
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class SystemTextJsonExtensions
{
	const string JsonSerializerRequirement =
		"Resolves the contract for T reflectively. Use the JsonTypeInfo<T> overload in a trimmed or Native "
		+ "AOT application, passing the type info from a source-generated JsonSerializerContext.";

	static readonly string[] EmptyPath = [];

	/// <summary>
	/// Deserializes JSON and validates it using a Zod schema, using a supplied contract.
	/// </summary>
	/// <typeparam name="T">The validated type.</typeparam>
	/// <param name="schema">The schema to validate with.</param>
	/// <param name="json">The JSON to deserialize.</param>
	/// <param name="jsonTypeInfo">
	/// The contract for <typeparamref name="T"/>, typically from a source-generated
	/// <see cref="JsonSerializerContext"/>.
	/// </param>
	/// <returns>The validated value, or the validation errors.</returns>
	/// <remarks>
	/// Safe under trimming and Native AOT: nothing here resolves a contract reflectively.
	/// </remarks>
	public static ValidationResult<T> DeserializeAndValidate<T>(
		this IZodSchema<T, T> schema,
		string json,
		JsonTypeInfo<T> jsonTypeInfo
	)
	{
		ArgumentNullException.ThrowIfNull(schema);
		ArgumentNullException.ThrowIfNull(json);
		ArgumentNullException.ThrowIfNull(jsonTypeInfo);

		try
		{
			var deserialized = JsonSerializer.Deserialize(json, jsonTypeInfo);

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
	/// Deserializes JSON from a stream and validates it using a Zod schema, using a supplied contract (async).
	/// </summary>
	/// <typeparam name="T">The validated type.</typeparam>
	/// <param name="schema">The schema to validate with.</param>
	/// <param name="jsonStream">The stream to read JSON from.</param>
	/// <param name="jsonTypeInfo">
	/// The contract for <typeparamref name="T"/>, typically from a source-generated
	/// <see cref="JsonSerializerContext"/>.
	/// </param>
	/// <param name="cancellationToken">Cancels the read and the validation.</param>
	/// <returns>The validated value, or the validation errors.</returns>
	/// <remarks>
	/// Safe under trimming and Native AOT: nothing here resolves a contract reflectively.
	/// </remarks>
	public static async ValueTask<ValidationResult<T>> DeserializeAndValidateAsync<T>(
		this IZodSchema<T, T> schema,
		Stream jsonStream,
		JsonTypeInfo<T> jsonTypeInfo,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(schema);
		ArgumentNullException.ThrowIfNull(jsonStream);
		ArgumentNullException.ThrowIfNull(jsonTypeInfo);

		try
		{
			var deserialized = await JsonSerializer
				.DeserializeAsync(jsonStream, jsonTypeInfo, cancellationToken)
				.ConfigureAwait(false);

			return deserialized == null
				? ValidationResult<T>.Failure(
					new ValidationError("deserialization_failed", "Failed to deserialize JSON", EmptyPath)
				)
				: await schema.ValidateAsync(deserialized, cancellationToken).ConfigureAwait(false);
		}
		catch (JsonException ex)
		{
			return ValidationResult<T>.Failure(
				new ValidationError("json_error", $"JSON parsing error: {ex.Message}", EmptyPath)
			);
		}
	}

	/// <summary>
	/// Validates a value and serializes it to a JSON string, using a supplied contract.
	/// </summary>
	/// <typeparam name="T">The validated type.</typeparam>
	/// <param name="schema">The schema to validate with.</param>
	/// <param name="value">The value to validate and serialize.</param>
	/// <param name="jsonTypeInfo">
	/// The contract for <typeparamref name="T"/>, typically from a source-generated
	/// <see cref="JsonSerializerContext"/>.
	/// </param>
	/// <returns>The serialized JSON, or the validation errors.</returns>
	/// <remarks>
	/// Safe under trimming and Native AOT: nothing here resolves a contract reflectively.
	/// </remarks>
	public static ValidationResult<string> ValidateAndSerialize<T>(
		this IZodSchema<T, T> schema,
		T value,
		JsonTypeInfo<T> jsonTypeInfo
	)
	{
		ArgumentNullException.ThrowIfNull(schema);
		ArgumentNullException.ThrowIfNull(jsonTypeInfo);

		var result = schema.Validate(value);

		return result.IsSuccess
			? ValidationResult<string>.Success(JsonSerializer.Serialize(result.Value, jsonTypeInfo))
			: ValidationResult<string>.Failure(result.Errors);
	}

	/// <summary>
	/// Validates a value and serializes it to a stream, using a supplied contract (async).
	/// </summary>
	/// <typeparam name="T">The validated type.</typeparam>
	/// <param name="schema">The schema to validate with.</param>
	/// <param name="value">The value to validate and serialize.</param>
	/// <param name="output">The stream to write to.</param>
	/// <param name="jsonTypeInfo">
	/// The contract for <typeparamref name="T"/>, typically from a source-generated
	/// <see cref="JsonSerializerContext"/>.
	/// </param>
	/// <param name="cancellationToken">Cancels the write.</param>
	/// <returns>An empty success, or the validation errors.</returns>
	/// <remarks>
	/// Safe under trimming and Native AOT: nothing here resolves a contract reflectively.
	/// </remarks>
	public static async ValueTask<ValidationResult<string>> ValidateAndSerializeAsync<T>(
		this IZodSchema<T, T> schema,
		T value,
		Stream output,
		JsonTypeInfo<T> jsonTypeInfo,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(schema);
		ArgumentNullException.ThrowIfNull(output);
		ArgumentNullException.ThrowIfNull(jsonTypeInfo);

		var result = await schema.ValidateAsync(value, cancellationToken).ConfigureAwait(false);
		if (!result.IsSuccess)
			return ValidationResult<string>.Failure(result.Errors);

		await JsonSerializer
			.SerializeAsync(output, result.Value, jsonTypeInfo, cancellationToken)
			.ConfigureAwait(false);

		return ValidationResult<string>.Success(string.Empty);
	}

	/// <summary>
	/// Deserializes JSON and validates it using a Zod schema.
	/// </summary>
	/// <remarks>
	/// Not supported under trimming or Native AOT, because the contract for <typeparamref name="T"/> is
	/// resolved reflectively. Use the <see cref="JsonTypeInfo{T}"/> overload there, passing the type info
	/// from a source-generated <see cref="JsonSerializerContext"/>.
	/// </remarks>
	[RequiresUnreferencedCode(JsonSerializerRequirement)]
	[RequiresDynamicCode(JsonSerializerRequirement)]
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
	/// <remarks>
	/// Not supported under trimming or Native AOT. Use the <see cref="JsonTypeInfo{T}"/> overload there.
	/// </remarks>
	[RequiresUnreferencedCode(JsonSerializerRequirement)]
	[RequiresDynamicCode(JsonSerializerRequirement)]
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
			// Older System.Text.Json packages lack the CancellationToken overload of DeserializeAsync.
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
				: await schema.ValidateAsync(deserialized, cancellationToken);
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
	/// <remarks>
	/// Not supported under trimming or Native AOT. Use the <see cref="JsonTypeInfo{T}"/> overload there.
	/// </remarks>
	[RequiresUnreferencedCode(JsonSerializerRequirement)]
	[RequiresDynamicCode(JsonSerializerRequirement)]
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
	/// <remarks>
	/// Not supported under trimming or Native AOT. Use the <see cref="JsonTypeInfo{T}"/> overload there.
	/// </remarks>
	[RequiresUnreferencedCode(JsonSerializerRequirement)]
	[RequiresDynamicCode(JsonSerializerRequirement)]
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

		var result = await schema.ValidateAsync(value, cancellationToken);
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
