namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for base64url (URL-safe base64) string format.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct Base64UrlRule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_string";

	/// <summary>Gets the message format; <c>{0}</c> is the offending value.</summary>
	public const string MessageFormat = "Invalid base64url string: {0}";

	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the Base64UrlRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public Base64UrlRule(string? message = null) => _message = message.OrNull();

	/// <summary>
	/// Validates that the value is a base64url string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value is not null && IsValid(value.AsSpan());

	/// <summary>
	/// Validates that the span is a base64url string without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value)
	{
		// Zod base64url: groups of 4 URL-safe base64 characters, optionally followed by a 2-3 character
		// tail. No padding is allowed.
		if (value.Length % 4 == 1)
			return false;

		foreach (var c in value)
		{
			if (!char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')
				return false;
		}

		return true;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? RuleMessage.Format(MessageFormat, value);

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) =>
		_message ?? RuleMessage.Format(MessageFormat, value.ToString());

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;
}
