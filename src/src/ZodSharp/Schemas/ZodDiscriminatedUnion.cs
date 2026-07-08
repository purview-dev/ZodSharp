using System.Collections.Immutable;
using System.Reflection;
using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for discriminated unions.
/// More efficient than regular unions when a discriminator field is present.
/// Equivalent to Zod's discriminatedUnion method.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ZodDiscriminatedUnion class.
/// </remarks>
/// <param name="discriminator">The discriminator field name</param>
/// <param name="options">The union options</param>
public class ZodDiscriminatedUnion(
	string discriminator,
	ImmutableDictionary<string, IZodSchema<object, object>> options
) : ZodType<object, object>
{
	/// <summary>
	/// Parses and validates the value using the discriminated union.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<object> ParseInternal(object value)
	{
		if (value == null)
		{
			return ValidationResult<object>.Failure(
				new ValidationError("invalid_type", "Expected object, but got null", [])
			);
		}

		var discriminatorValue = GetDiscriminatorValue(value);

		if (discriminatorValue is null)
		{
			return ValidationResult<object>.Failure(
				new ValidationError("missing_discriminator", $"Discriminator field '{discriminator}' not found", [])
			);
		}

		if (!options.TryGetValue(discriminatorValue, out var schema))
		{
			return ValidationResult<object>.Failure(
				new ValidationError(
					"invalid_discriminator",
					$"Invalid discriminator value '{discriminatorValue}'. Expected one of: {string.Join(", ", options.Keys)}",
					[]
				)
			);
		}

		// Success!
		return schema.Validate(value);
	}

	string? GetDiscriminatorValue(object value)
	{
		if (value is IReadOnlyDictionary<string, object?> readOnlyDictionary)
		{
			return TryGetDictionaryDiscriminatorValue(readOnlyDictionary);
		}

		if (value is IDictionary<string, object?> dictionary)
		{
			return TryGetDictionaryDiscriminatorValue(dictionary);
		}

		var property = value
			.GetType()
			.GetProperty(discriminator, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

		return property?.GetValue(value)?.ToString();
	}

	string? TryGetDictionaryDiscriminatorValue(IEnumerable<KeyValuePair<string, object?>> dictionary)
	{
		foreach (var (key, dictionaryValue) in dictionary)
		{
			if (string.Equals(key, discriminator, StringComparison.OrdinalIgnoreCase))
			{
				return dictionaryValue?.ToString();
			}
		}

		return null;
	}
}
