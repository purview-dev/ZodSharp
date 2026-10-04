namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for a Uri.
/// <strong>This will allocate by utilising <see cref="Uri.TryCreate(string?, UriKind, out Uri?)"/></strong>
/// </summary>
[Core.ZodRule]
public readonly record struct UriRule : Core.IValidationRule<string>, Core.IZodRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_string";

	/// <summary>Gets the message format; <c>{0}</c> is the <see cref="UriKind"/> and <c>{1}</c> the value.</summary>
	public const string MessageFormat = "Invalid Uri, kind: {0}, format: {1}";

	readonly string? _message;
	readonly UriKind _uriKind;

	/// <summary>
	/// Initializes a new instance of the UrlRule struct.
	/// </summary>
	/// <param name="uriKind">The uri kind to validate against</param>
	/// <param name="message">Optional error message</param>
	public UriRule(UriKind uriKind, string? message = null)
	{
		_uriKind = uriKind;
		_message = message.OrNull();
	}

	/// <summary>
	/// Validates that the value is a valid URL.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) =>
		!string.IsNullOrWhiteSpace(value) && Uri.TryCreate(value, _uriKind, out var _);

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? RuleMessage.Format(MessageFormat, _uriKind, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => ErrorCode;

	string? Core.IZodRule.Origin => null;
}
