using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema wrapper that makes a value optional.
/// </summary>
/// <typeparam name="T">The inner schema type</typeparam>
public class ZodOptional<T> : ZodType<T?, T?>
	where T : class
{
	readonly IZodSchema<T, T> _innerSchema;

	/// <summary>
	/// Initializes a new instance of the ZodOptional class.
	/// </summary>
	/// <param name="innerSchema">The inner schema</param>
	public ZodOptional(IZodSchema<T, T> innerSchema)
	{
		_innerSchema = innerSchema;
	}

	/// <summary>
	/// Parses and validates the value, allowing null.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<T?> ParseInternal(T? value)
	{
		if (value == null)
		{
			return ValidationResult<T?>.Success(null);
		}

		var result = _innerSchema.Validate(value);
		if (!result.IsSuccess)
		{
			return ValidationResult<T?>.Failure(result.Errors);
		}

		return ValidationResult<T?>.Success(result.Value);
	}
}
