using System.Text.RegularExpressions;

namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for regex pattern matching.
/// Uses struct to avoid allocations.
/// </summary>
[Core.ZodRule]
public readonly record struct RegexRule : Core.IValidationRule<string>, Core.IStringValidationRule, Core.IZodRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_string";

	/// <summary>Gets the message format; <c>{0}</c> is the required pattern.</summary>
	public const string MessageFormat = "String does not match the required pattern: {0}";

	readonly Regex _pattern;
	readonly string _message;
	readonly string _code;

	/// <summary>
	/// Initializes a new instance of the RegexRule struct.
	/// </summary>
	/// <param name="pattern">The regex pattern</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	public RegexRule(Regex pattern, string? message = null, string? code = null)
	{
		_pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
		_message = message.Or(MessageFormat);
		_code = code.Or(ErrorCode);
	}

	/// <summary>
	/// Initializes a new instance of the RegexRule struct.
	/// </summary>
	/// <param name="pattern">The regex pattern</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	public RegexRule(string pattern, string? message = null, string? code = null)
		: this(new Regex(pattern, RegexOptions.Compiled), message, code) { }

	/// <summary>
	/// Validates that the value matches the regex pattern.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => _pattern.IsMatch(value);

	/// <summary>
	/// Validates that the span matches the regex pattern without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value) => _pattern.IsMatch(value);

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
	public string GetErrorMessage(ReadOnlySpan<char> value) => GetErrorMessage(value.ToString());

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => _code;

	string? Core.IZodRule.Origin => null;
}
