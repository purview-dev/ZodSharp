using System.Text.RegularExpressions;

namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for email format.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct EmailRule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_string";

	/// <summary>Gets the message format; <c>{0}</c> is the offending value.</summary>
	public const string MessageFormat = "Invalid email format: {0}";

	/// <summary>
	/// Regular expression for validating email format.
	/// </summary>
	public static readonly Regex EmailRegex = new(
		@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
		RegexOptions.Compiled | RegexOptions.IgnoreCase,
		TimeSpan.FromMilliseconds(100)
	);

	/// <summary>
	/// Validates that the value matches the email format.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => EmailRegex.IsMatch(value);

	/// <summary>
	/// Validates that the span matches the email format without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value) => EmailRegex.IsMatch(value);

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => RuleMessage.Format(MessageFormat, value);

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) => RuleMessage.Format(MessageFormat, value.ToString());

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;
}
