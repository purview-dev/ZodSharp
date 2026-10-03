namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for minimum string length.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct MinLengthRule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "too_small";

	/// <summary>Gets the message format; <c>{0}</c> is the minimum length and <c>{1}</c> the actual length.</summary>
	public const string MessageFormat = "String must be at least {0} characters long, but got {1}";

	readonly int _minLength;

	/// <summary>
	/// Initializes a new instance of the MinLengthRule struct.
	/// </summary>
	/// <param name="minLength">The minimum length</param>
	public MinLengthRule(int minLength)
	{
		_minLength = minLength;
	}

	/// <summary>
	/// Validates that the value meets the minimum length requirement.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value.LengthOrDefault() >= _minLength;

	/// <summary>
	/// Validates that the span meets the minimum length requirement without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value) => value.Length >= _minLength;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) =>
		RuleMessage.Format(MessageFormat, _minLength, value.LengthOrDefault());

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) =>
		RuleMessage.Format(MessageFormat, _minLength, value.Length);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;
}
