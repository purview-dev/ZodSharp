namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for finite number check.
/// Uses struct to avoid allocations.
/// </summary>
[Core.ZodRule]
public readonly record struct FiniteRule : Core.IValidationRule<double>, Core.IZodRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "not_finite";

	/// <summary>Gets the message format; <c>{0}</c> is the offending value.</summary>
	public const string MessageFormat = "Number must be finite, but got {0}";

	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the FiniteRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public FiniteRule(string? message = null)
	{
		_message = message.OrNull();
	}

	/// <summary>
	/// Validates that the value is finite.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in double value) => double.IsFinite(value);

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in double value) => _message ?? RuleMessage.Format(MessageFormat, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => ErrorCode;

	string? Core.IZodRule.Origin => null;
}
