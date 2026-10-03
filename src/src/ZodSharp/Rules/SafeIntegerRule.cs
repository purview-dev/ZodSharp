namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for safe integer check.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct SafeIntegerRule : Core.IValidationRule<double>
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "too_big";

	/// <summary>Gets the message format; <c>{0}</c> is the offending value.</summary>
	public const string MessageFormat = "Number must be a safe integer, but got {0}";

	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the SafeIntegerRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public SafeIntegerRule(string? message = null)
	{
		_message = message.OrNull();
	}

	/// <summary>
	/// Validates that the value is a safe integer (within int.MinValue and int.MaxValue).
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in double value) =>
		value == Math.Truncate(value) && value >= int.MinValue && value <= int.MaxValue;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in double value) => _message ?? RuleMessage.Format(MessageFormat, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;
}
