using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for record (dictionary) validation. Validates that all values in a
/// dictionary match the value schema. Equivalent to Zod's
/// <c>z.record(valueSchema)</c>.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ZodRecord{TValue}"/> class.
/// </remarks>
/// <param name="valueSchema">The schema for all values.</param>
public class ZodRecord<TValue>(IZodSchema<TValue, TValue> valueSchema)
	: ZodType<Dictionary<string, TValue>, Dictionary<string, TValue>>
{
	static readonly string[] EmptyPath = [];

	/// <summary>
	/// Validates that all values in the dictionary match the value schema.
	/// </summary>
	/// <param name="value">The dictionary to validate.</param>
	/// <returns>A validation result.</returns>
	protected override ValidationResult<Dictionary<string, TValue>> ParseInternal(
		Dictionary<string, TValue> value
	)
	{
		if (value == null)
		{
			return ValidationResult<Dictionary<string, TValue>>.Failure(
				new ValidationError("invalid_type", "Expected record, but got null", EmptyPath)
			);
		}

		List<ValidationError> errors = [];
		Dictionary<string, TValue> validated = [];

		foreach (var (key, itemValue) in value)
		{
			if (itemValue is null)
			{
				validated[key] = default!;
				continue;
			}

			var result = valueSchema.Validate(itemValue);
			if (!result.IsSuccess)
			{
				foreach (var error in result.Errors)
				{
					var path = new string[error.Path.Length + 1];
					path[0] = key;
					error.Path.CopyTo(0, path, 1, error.Path.Length);
					errors.Add(new(error.Code, error.Message, path, error.Parameters));
				}
			}
			else
			{
				validated[key] = result.Value!;
			}
		}

		return errors.Count > 0
			? ValidationResult<Dictionary<string, TValue>>.Failure(errors)
			: ValidationResult<Dictionary<string, TValue>>.Success(validated);
	}
}
