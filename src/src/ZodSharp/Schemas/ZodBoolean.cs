using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for boolean validation.
/// </summary>
public class ZodBoolean : ZodType<bool>
{
	/// <summary>
	/// Parses and validates a boolean value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<bool> ParseInternal(bool value) => ValidationResult<bool>.Success(value);
}
