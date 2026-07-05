using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for null validation.
/// </summary>
public class ZodNull : ZodType<object?>
{
	/// <summary>
	/// Parses and validates a null value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<object?> ParseInternal(object? value) =>
		value == null
			? ValidationResult<object?>.Success(null)
			: ValidationResult<object?>.Failure(
				new ValidationError("invalid_type", "Expected null, but got non-null value", [])
			);
}
