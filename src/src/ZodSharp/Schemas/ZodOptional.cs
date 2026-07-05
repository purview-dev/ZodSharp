using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema wrapper that makes a value optional.
/// </summary>
/// <typeparam name="T">The inner schema type</typeparam>
/// <remarks>
/// Initializes a new instance of the ZodOptional class.
/// </remarks>
/// <param name="innerSchema">The inner schema</param>
public class ZodOptional<T>(IZodSchema<T, T> innerSchema) : ZodType<T?, T?>
	where T : class
{
	/// <summary>
	/// Parses and validates the value, allowing null.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<T?> ParseInternal(T? value)
	{
		if (value == null)
			return ValidationResult<T?>.Success(null);

		var result = innerSchema.Validate(value);
		return result.IsSuccess
			? ValidationResult<T?>.Success(result.Value)
			: ValidationResult<T?>.Failure(result.Errors);
	}
}
