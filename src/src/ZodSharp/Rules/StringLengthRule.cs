namespace ZodSharp.Rules;

/// <summary>
/// Validation rule that requires a string to have a maximum length and an optional minimum length,
/// mirroring <c>System.ComponentModel.DataAnnotations.StringLengthAttribute</c>.
/// </summary>
/// <remarks>
/// The generated attribute is named <c>[StringLengthZod]</c> because <c>StringLengthAttribute</c> is already
/// declared by <c>System.ComponentModel.DataAnnotations</c>.
/// </remarks>
[Core.ZodRule]
public readonly record struct StringLengthRule : Core.IValidationRule<string>, Core.IStringValidationRule, Core.IZodRule
{
	/// <summary>Gets the error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_length";

	/// <summary>Gets the message format; <c>{0}</c> is the maximum, <c>{1}</c> the minimum and <c>{2}</c> the actual length.</summary>
	public const string MessageFormat = "String must be at most {0} characters long and at least {1}, but got {2}";

	readonly int _maximumLength;
	readonly int _minimumLength;
	readonly string _message;

	/// <summary>
	/// Initializes a new instance of the <see cref="StringLengthRule"/> struct.
	/// </summary>
	/// <param name="maximumLength">The inclusive maximum length.</param>
	/// <param name="minimumLength">The inclusive minimum length. Defaults to 0.</param>
	/// <param name="message">Optional error message/ message format.</param>
	/// <param name="code">Optional error code override. If one is not specified then the <see cref="ErrorCode"/> is used.</param>
	public StringLengthRule(int maximumLength, int minimumLength = 0, string? message = null, string? code = null)
	{
		_maximumLength = maximumLength;
		_minimumLength = minimumLength;
		_message = message.Or(MessageFormat);
		Code = code.Or(ErrorCode);
	}

	/// <summary>
	/// Validates that the value's length is within the configured range.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value)
	{
		var length = value.LengthOrDefault();
		return length >= _minimumLength && length <= _maximumLength;
	}

	/// <summary>
	/// Validates that the span's length is within the configured range without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value) => value.Length >= _minimumLength && value.Length <= _maximumLength;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) =>
		RuleMessage.Format(_message ?? MessageFormat, _maximumLength, _minimumLength, value.LengthOrDefault());

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) =>
		RuleMessage.Format(_message ?? MessageFormat, _maximumLength, _minimumLength, value.Length);

	/// <summary>Gets the error code reported when the rule fails.</summary>
	public string Code => field.Or(ErrorCode);

	string? Core.IZodRule.Code => Code;

	string? Core.IZodRule.Origin => null;
}
