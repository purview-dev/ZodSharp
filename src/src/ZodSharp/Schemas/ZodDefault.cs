using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema that provides a default value when input is null or missing.
/// Equivalent to Zod's default method.
/// </summary>
/// <typeparam name="T">The type</typeparam>
public class ZodDefault<T> : ZodType<T, T?>
{
	readonly IZodSchema<T> _innerSchema;
	readonly T _defaultValue;

	/// <summary>
	/// Initializes a new instance of the ZodDefault class.
	/// </summary>
	/// <param name="innerSchema">The inner schema</param>
	/// <param name="defaultValue">The default value</param>
	public ZodDefault(IZodSchema<T> innerSchema, T defaultValue)
	{
		_innerSchema = innerSchema;
		_defaultValue = defaultValue;
	}

	/// <summary>
	/// Parses and validates the value, using the default if null.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<T> ParseInternal(T? value)
	{
		if (value == null)
		{
			return ValidationResult<T>.Success(_defaultValue);
		}

		return _innerSchema.Validate(value);
	}
}
