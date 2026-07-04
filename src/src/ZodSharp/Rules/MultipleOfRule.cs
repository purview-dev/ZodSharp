namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for multiple-of check.
/// Uses struct to avoid allocations.
/// </summary>
public readonly struct MultipleOfRule : Core.IValidationRule<double>
{
	readonly double _divisor;
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the MultipleOfRule struct.
	/// </summary>
	/// <param name="divisor">The divisor</param>
	/// <param name="message">Optional error message</param>
	public MultipleOfRule(double divisor, string? message = null)
	{
		if (divisor == 0)
			throw new ArgumentException("Divisor cannot be zero", nameof(divisor));
		_divisor = divisor;
		_message = message;
	}

	/// <summary>
	/// Validates that the value is a multiple of the divisor.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in double value)
	{
		var remainder = Math.Abs(value % _divisor);
		return remainder < double.Epsilon || Math.Abs(remainder - _divisor) < double.Epsilon;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in double value)
	{
		return _message ?? $"Number must be a multiple of {_divisor}, but got {value}";
	}
}
