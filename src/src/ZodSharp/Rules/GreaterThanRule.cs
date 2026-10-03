namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for an exclusive minimum numeric value (strictly greater than).
/// Uses struct to avoid allocations.
/// </summary>
/// <typeparam name="T">The numeric type</typeparam>
public readonly record struct GreaterThanRule<T> : Core.IValidationRule<T>
	where T : IComparable<T>
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "too_small";

	/// <summary>Gets the message format; <c>{0}</c> is the bound and <c>{1}</c> the offending value.</summary>
	public const string MessageFormat = "Value must be greater than {0}, but got {1}";

	readonly T _exclusiveMinimum;

	/// <summary>
	/// Initializes a new instance of the GreaterThanRule struct.
	/// </summary>
	/// <param name="exclusiveMinimum">The value the input must be strictly greater than</param>
	public GreaterThanRule(T exclusiveMinimum)
	{
		_exclusiveMinimum = exclusiveMinimum;
	}

	/// <summary>
	/// Validates that the value is strictly greater than the configured bound.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in T value) => value.CompareTo(_exclusiveMinimum) > 0;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in T value) => RuleMessage.Format(MessageFormat, _exclusiveMinimum, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;
}
