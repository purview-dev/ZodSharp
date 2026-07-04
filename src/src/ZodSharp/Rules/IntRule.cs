namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for integer values.
/// Uses struct to avoid allocations.
/// </summary>
public readonly struct IntRule : Core.IValidationRule<double>
{
	/// <summary>
	/// Validates that the value is an integer.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in double value)
	{
		return value == Math.Truncate(value);
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in double value)
	{
		return $"Expected integer, but got {value}";
	}
}
