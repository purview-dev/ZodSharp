using System.Collections.Immutable;
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

		var type = value.GetType();
		var discriminatorProperty = type.GetProperty(discriminator);

		if (discriminatorProperty == null)
		{
			return ValidationResult<object>.Failure(
				new ValidationError("missing_discriminator", $"Discriminator field '{discriminator}' not found", [])
			);
		}

		var discriminatorValue = discriminatorProperty.GetValue(value)?.ToString();

		return discriminatorValue == null || !options.TryGetValue(discriminatorValue, out var schema)
			? ValidationResult<object>.Failure(
				new ValidationError(
					"invalid_discriminator",
					$"Invalid discriminator value '{discriminatorValue}'. Expected one of: {string.Join(", ", options.Keys)}",
					[]
				)
			)
			: schema.Validate(value);
	}
}
