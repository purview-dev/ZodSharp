using System.Collections.Immutable;
using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for discriminated unions.
/// More efficient than regular unions when a discriminator field is present.
/// Equivalent to Zod's discriminatedUnion method.
/// </summary>
public class ZodDiscriminatedUnion : ZodType<object, object>
{
	readonly string _discriminator;
	readonly ImmutableDictionary<string, IZodSchema<object, object>> _options;

	/// <summary>
	/// Initializes a new instance of the ZodDiscriminatedUnion class.
	/// </summary>
	/// <param name="discriminator">The discriminator field name</param>
	/// <param name="options">The union options</param>
	public ZodDiscriminatedUnion(string discriminator, ImmutableDictionary<string, IZodSchema<object, object>> options)
	{
		_discriminator = discriminator;
		_options = options;
	}

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
				new ValidationError("invalid_type", "Expected object, but got null", Array.Empty<string>())
			);
		}

		var type = value.GetType();
		var discriminatorProperty = type.GetProperty(_discriminator);

		if (discriminatorProperty == null)
		{
			return ValidationResult<object>.Failure(
				new ValidationError(
					"missing_discriminator",
					$"Discriminator field '{_discriminator}' not found",
					Array.Empty<string>()
				)
			);
		}

		var discriminatorValue = discriminatorProperty.GetValue(value)?.ToString();

		if (discriminatorValue == null || !_options.TryGetValue(discriminatorValue, out var schema))
		{
			return ValidationResult<object>.Failure(
				new ValidationError(
					"invalid_discriminator",
					$"Invalid discriminator value '{discriminatorValue}'. Expected one of: {string.Join(", ", _options.Keys)}",
					Array.Empty<string>()
				)
			);
		}

		return schema.Validate(value);
	}
}

/// <summary>
/// Builder for creating discriminated unions.
/// </summary>
public class ZodDiscriminatedUnionBuilder
{
	readonly string _discriminator;
	readonly Dictionary<string, IZodSchema<object, object>> _options = new();

	/// <summary>
	/// Initializes a new instance of the ZodDiscriminatedUnionBuilder class.
	/// </summary>
	/// <param name="discriminator">The discriminator field name</param>
	public ZodDiscriminatedUnionBuilder(string discriminator)
	{
		_discriminator = discriminator;
	}

	/// <summary>
	/// Adds an option to the discriminated union.
	/// </summary>
	/// <param name="value">The discriminator value</param>
	/// <param name="schema">The schema for this option</param>
	/// <returns>This builder for method chaining</returns>
	public ZodDiscriminatedUnionBuilder Option(string value, IZodSchema<object, object> schema)
	{
		_options[value] = schema;
		return this;
	}

	/// <summary>
	/// Builds the discriminated union schema.
	/// </summary>
	public ZodDiscriminatedUnion Build()
	{
		return new ZodDiscriminatedUnion(_discriminator, _options.ToImmutableDictionary());
	}
}
