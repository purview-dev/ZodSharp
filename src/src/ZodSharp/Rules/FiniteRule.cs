namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for finite number check.
/// Uses struct to avoid allocations.
/// </summary>
public readonly struct FiniteRule : Core.IValidationRule<double>
{
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the FiniteRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public FiniteRule(string? message = null)
	{
		_message = message;
	}

	/// <summary>
	/// Validates that the value is finite.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in double value)
	{
		return double.IsFinite(value);
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in double value)
	{
		return _message ?? $"Number must be finite, but got {value}";
	}
}
