namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for safe integer check.
/// Uses struct to avoid allocations.
/// </summary>
[Core.ZodRule]
public readonly record struct SafeIntegerRule : Core.IValidationRule<double>, Core.IZodRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "too_big";

	/// <summary>Gets the message format; <c>{0}</c> is the offending value.</summary>
	public const string MessageFormat = "Number must be a safe integer, but got {0}";

	readonly string _message;
	readonly string _code;

	/// <summary>
	/// Initializes a new instance of the SafeIntegerRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	public SafeIntegerRule(string? message = null, string? code = null)
	{
		_message = message.Or(MessageFormat);
		_code = code.Or(ErrorCode);
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
	public string GetErrorMessage(in double value) => RuleMessage.Format(_message ?? MessageFormat, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => _code;

	string? Core.IZodRule.Origin => null;
}
