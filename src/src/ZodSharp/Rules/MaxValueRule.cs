namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for maximum numeric value.
/// Uses struct to avoid allocations.
/// </summary>
/// <typeparam name="T">The numeric type</typeparam>
public readonly record struct MaxValueRule<T> : Core.IValidationRule<T>
	where T : IComparable<T>
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "too_big";

	/// <summary>Gets the message format; <c>{0}</c> is the bound and <c>{1}</c> the offending value.</summary>
	public const string MessageFormat = "Value must be at most {0}, but got {1}";

	readonly T _maxValue;

	/// <summary>
	/// Initializes a new instance of the MaxValueRule struct.
	/// </summary>
	/// <param name="maxValue">The maximum value</param>
	public MaxValueRule(T maxValue)
	{
		_maxValue = maxValue;
	}

	/// <summary>
	/// Validates that the value is less than or equal to the maximum value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in T value) => value.CompareTo(_maxValue) <= 0;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in T value) => RuleMessage.Format(MessageFormat, _maxValue, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;
}
