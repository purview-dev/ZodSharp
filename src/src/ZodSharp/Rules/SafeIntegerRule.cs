namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for safe integer check.
/// Uses struct to avoid allocations.
/// </summary>
public readonly struct SafeIntegerRule : Core.IValidationRule<double>
{
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the SafeIntegerRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public SafeIntegerRule(string? message = null)
	{
		_message = message;
	}

	/// <summary>
	/// Validates that the value is a safe integer (within int.MinValue and int.MaxValue).
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in double value)
	{
		return value == Math.Truncate(value) && value >= int.MinValue && value <= int.MaxValue;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in double value)
	{
		return _message ?? $"Number must be a safe integer, but got {value}";
	}
}
