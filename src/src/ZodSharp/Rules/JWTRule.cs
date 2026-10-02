namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for JSON Web Token (JWT) format: three base64url-encoded segments separated by periods.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct JWTRule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the JWTRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public JWTRule(string? message = null) => _message = message.OrNull();

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
	public string GetErrorMessage(in string value) => _message ?? $"Invalid JWT: {value}";

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) => _message ?? $"Invalid JWT: {value}";

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
	public string Code => "invalid_string";
}
