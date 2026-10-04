namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for JSON Web Token (JWT) format: three base64url-encoded segments separated by periods.
/// Uses struct to avoid allocations.
/// </summary>
[Core.ZodRule]
public readonly record struct JWTRule : Core.IValidationRule<string>, Core.IStringValidationRule, Core.IZodRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_string";

	/// <summary>Gets the message format; <c>{0}</c> is the offending value.</summary>
	public const string MessageFormat = "Invalid JWT: {0}";

	readonly string _message;
	readonly string _code;

	/// <summary>
	/// Initializes a new instance of the JWTRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	public JWTRule(string? message = null, string? code = null)
	{
		_message = message.Or(MessageFormat);
		_code = code.Or(ErrorCode);
	}

	/// <summary>
	/// Validates that the value is a JWT.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value is not null && IsValid(value.AsSpan());

	/// <summary>
	/// Validates that the span is a JWT without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value)
	{
		if (value.IsEmpty || value.IsWhiteSpace())
			return false;

		var firstDot = value.IndexOf('.');
		if (firstDot <= 0)
			return false;

		var secondDot = value[(firstDot + 1)..].IndexOf('.');
		if (secondDot <= 0)
			return false;

		secondDot += firstDot + 1;

		return IsBase64Url(value[..firstDot])
			&& IsBase64Url(value[(firstDot + 1)..secondDot])
			&& IsBase64Url(value[(secondDot + 1)..]);
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => RuleMessage.Format(_message ?? MessageFormat, value);

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) =>
		RuleMessage.Format(_message ?? MessageFormat, value.ToString());

	static bool IsBase64Url(ReadOnlySpan<char> segment)
	{
		if (segment.IsEmpty)
			return false;

		foreach (var c in segment)
		{
			if (!char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')
				return false;
		}

		return true;
	}

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => _code;

	string? Core.IZodRule.Origin => null;
}
