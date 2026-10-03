namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for maximum string length.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct MaxLengthRule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "too_big";

	/// <summary>Gets the message format; <c>{0}</c> is the maximum length and <c>{1}</c> the actual length.</summary>
	public const string MessageFormat = "String must be at most {0} characters long, but got {1}";

	readonly int _maxLength;

	/// <summary>
	/// Initializes a new instance of the MaxLengthRule struct.
	/// </summary>
	/// <param name="maxLength">The maximum length</param>
	public MaxLengthRule(int maxLength)
	{
		_maxLength = maxLength;
	}

	/// <summary>
	/// Validates that the value meets the maximum length requirement.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value.LengthOrDefault() <= _maxLength;

	/// <summary>
	/// Validates that the span meets the maximum length requirement without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value) => value.Length <= _maxLength;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) =>
		RuleMessage.Format(MessageFormat, _maxLength, value.LengthOrDefault());

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) =>
		RuleMessage.Format(MessageFormat, _maxLength, value.Length);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;
}
