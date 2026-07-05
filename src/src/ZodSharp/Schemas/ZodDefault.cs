using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema that provides a default value when input is null or missing.
/// Equivalent to Zod's default method.
/// </summary>
/// <typeparam name="T">The type</typeparam>
/// <remarks>
/// Initializes a new instance of the ZodDefault class.
/// </remarks>
/// <param name="innerSchema">The inner schema</param>
/// <param name="defaultValue">The default value</param>
public class ZodDefault<T>(IZodSchema<T> innerSchema, T defaultValue) : ZodType<T, T?>
{
	/// <summary>
	/// Parses and validates the value, using the default if null.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<T> ParseInternal(T? value) =>
		value == null ? ValidationResult<T>.Success(defaultValue) : innerSchema.Validate(value);
}
