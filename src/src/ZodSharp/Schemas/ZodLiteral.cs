using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for literal value validation.
/// </summary>
/// <typeparam name="T">The literal type</typeparam>
/// <remarks>
/// Initializes a new instance of the ZodLiteral class.
/// </remarks>
/// <param name="value">The literal value</param>
public class ZodLiteral<T>(T value) : ZodType<T, T>
	where T : IEquatable<T>
{
	readonly T _value = value;

	/// <summary>
	/// Parses and validates the value against the literal.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<T> ParseInternal(T value) =>
		value.Equals(_value)
			? ValidationResult<T>.Success(value)
			: ValidationResult<T>.Failure(
				new ValidationError("invalid_literal", $"Expected literal value {_value}, but got {value}", [])
			);
}
