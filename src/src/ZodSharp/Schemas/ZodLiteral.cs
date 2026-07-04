using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for literal value validation.
/// </summary>
/// <typeparam name="T">The literal type</typeparam>
public class ZodLiteral<T> : ZodType<T, T>
	where T : IEquatable<T>
{
	readonly T _value;

	/// <summary>
	/// Initializes a new instance of the ZodLiteral class.
	/// </summary>
	/// <param name="value">The literal value</param>
	public ZodLiteral(T value)
	{
		_value = value;
	}

	/// <summary>
	/// Parses and validates the value against the literal.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<T> ParseInternal(T value)
	{
		if (!value.Equals(_value))
		{
			return ValidationResult<T>.Failure(
				new ValidationError(
					"invalid_literal",
					$"Expected literal value {_value}, but got {value}",
					Array.Empty<string>()
				)
			);
		}

		return ValidationResult<T>.Success(value);
	}
}
