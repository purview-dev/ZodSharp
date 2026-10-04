namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for string suffix.
/// Uses struct to avoid allocations.
/// </summary>
[Core.ZodRule]
public readonly record struct EndsWithRule : Core.IValidationRule<string>, Core.IStringValidationRule, Core.IZodRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_string";

	/// <summary>Gets the message format; <c>{0}</c> is the required suffix and <c>{1}</c> the value.</summary>
	public const string MessageFormat = "String must end with '{0}', but got '{1}'";

	readonly string _suffix;
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the EndsWithRule struct.
	/// </summary>
	/// <param name="suffix">The required suffix</param>
	/// <param name="message">Optional error message</param>
	public EndsWithRule(string suffix, string? message = null)
	{
		_suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
		_message = message.OrNull();
	}

	/// <summary>
	/// Validates that the value ends with the specified suffix.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value != null && value.EndsWith(_suffix, StringComparison.Ordinal);

	/// <summary>
	/// Validates that the span ends with the specified suffix without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value) => value.EndsWith(_suffix.AsSpan(), StringComparison.Ordinal);

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? RuleMessage.Format(MessageFormat, _suffix, value);

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) =>
		_message ?? RuleMessage.Format(MessageFormat, _suffix, value.ToString());

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => ErrorCode;

	string? Core.IZodRule.Origin => null;
}
