using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ZodSharp.Core;

namespace ZodSharp.Json;

/// <summary>
/// System.Text.Json converter that validates using a Zod schema.
/// </summary>
/// <remarks>
/// <para>
/// Reading and writing go through a <see cref="JsonTypeInfo{T}"/> resolved from the serializer options
/// rather than the reflection-based <see cref="JsonSerializer"/> overloads. Those overloads are annotated
/// <c>RequiresUnreferencedCode</c>/<c>RequiresDynamicCode</c> whatever the options contain, so using them
/// here would make every consumer of this package unsafe under trimming and Native AOT. Resolving the type
/// info instead defers to whatever resolver the host configured: a source-generated
/// <see cref="JsonSerializerContext"/> in a trimmed or AOT application, or the default reflection resolver
/// elsewhere. The converter itself adds no reflection.
/// </para>
/// <para>
/// The options copy that excludes this converter is built once per converter instance. It used to be
/// rebuilt on every read and write, which is not just allocation: a fresh
/// <see cref="JsonSerializerOptions"/> has a cold metadata cache, so each call re-resolved the contract for
/// <typeparamref name="T"/>.
/// </para>
/// </remarks>
sealed class ZodJsonConverter<T>(IZodSchema<T, T> schema) : JsonConverter<T>
{
	JsonSerializerOptions? _withoutThisConverter;
	JsonTypeInfo<T>? _typeInfo;

	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// Buffer the current token so we can re-read it after extracting the value.
		using var document = JsonDocument.ParseValue(ref reader);
		var deserialized =
			JsonSerializer.Deserialize(document.RootElement, TypeInfoFor(options))
			?? throw new JsonException("Failed to deserialize JSON");
		var result = schema.Validate(deserialized);
		if (!result.IsSuccess)
		{
			var errorMessages = string.Join(", ", result.Errors.Select(e => e.Message));
			throw new JsonException($"Validation failed: {errorMessages}");
		}

		return result.Value;
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}

		var result = schema.Validate(value);
		if (!result.IsSuccess)
		{
			var errorMessages = string.Join(", ", result.Errors.Select(e => e.Message));
			throw new JsonException($"Validation failed: {errorMessages}");
		}

		JsonSerializer.Serialize(writer, result.Value, TypeInfoFor(options));
	}

	/// <summary>
	/// Resolves the contract for <typeparamref name="T"/> from options that exclude this converter.
	/// </summary>
	/// <remarks>
	/// Cached per converter instance. A converter is registered against one options instance in practice, so
	/// the first resolution wins; a racing caller simply resolves the same contract again and the result is
	/// equivalent.
	/// </remarks>
	JsonTypeInfo<T> TypeInfoFor(JsonSerializerOptions options)
	{
		if (_typeInfo is { } cached)
			return cached;

		var effective = _withoutThisConverter ??= WithoutThisConverter(options);
		var resolved =
			effective.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
			?? throw new JsonException(
				$"No JsonTypeInfo is available for '{typeof(T)}'. In a trimmed or Native AOT application, "
					+ "register the type with a source-generated JsonSerializerContext and set it as the "
					+ "options' TypeInfoResolver."
			);

		_typeInfo = resolved;

		return resolved;
	}

	/// <summary>
	/// Creates a shallow copy of <paramref name="options"/> with this converter removed,
	/// preventing infinite recursion during (de)serialization.
	/// </summary>
	JsonSerializerOptions WithoutThisConverter(JsonSerializerOptions options)
	{
		JsonSerializerOptions copy = new(options);

		for (var i = copy.Converters.Count - 1; i >= 0; i--)
		{
			if (ReferenceEquals(copy.Converters[i], this))
			{
				copy.Converters.RemoveAt(i);
			}
		}

		return copy;
	}
}
