namespace ZodSharp.Rules;

/// <summary>
/// Validation rule that accepts a <see langword="null"/> string but rejects one that is empty or
/// whitespace-only.
/// </summary>
/// <remarks>
/// <para>
/// This is the null-tolerant counterpart to <see cref="NonSentinelRule{T}"/> (which treats a null string as
/// a sentinel) and <see cref="RequiredRule{T}"/> (which always rejects null). A nullable scalar value object
/// round-trips JSON <c>null</c>, so its schema must accept <c>null</c> while still rejecting a whitespace-only
/// value; the predicate is <c>value is null || !string.IsNullOrWhiteSpace(value)</c>.
/// </para>
/// <para>
/// A rule written against the underlying <c>string?</c> value is adapted to a scalar value object by the
/// <c>ScalarRuleAdapter</c> the value-object generator emits, so <c>[NullOrNonWhiteSpace]</c> validates a
/// <c>[Scalar&lt;string&gt;(Nullable = true)]</c> (or <c>[Scalar(typeof(string), Nullable = true)]</c>) type
/// as a unit.
/// </para>
/// </remarks>
[Core.ZodRule]
public readonly record struct NullOrNonWhiteSpaceRule : Core.IValidationRule<string?>, Core.IZodRule
{
	/// <summary>Gets the error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_string";

	/// <summary>Gets the message format; <c>{0}</c> is the offending value.</summary>
	public const string MessageFormat = "Value must be null or non-whitespace, but got '{0}'";

	readonly string _message;

	/// <summary>
	/// Initializes a new instance of the <see cref="NullOrNonWhiteSpaceRule"/> struct.
	/// </summary>
	/// <param name="message">Optional error message/ message format.</param>
	/// <param name="code">Optional error code override. If one is not specified then the <see cref="ErrorCode"/> is used.</param>
	public NullOrNonWhiteSpaceRule(string? message = null, string? code = null)
	{
		_message = message.Or(MessageFormat);
		Code = code.Or(ErrorCode);
	}

	/// <summary>
	/// Validates that the value is <see langword="null"/> or contains non-whitespace characters.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string? value) => value is null || !string.IsNullOrWhiteSpace(value);

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string? value) => RuleMessage.Format(_message ?? MessageFormat, value);

	/// <summary>Gets the error code reported when the rule fails.</summary>
	public string Code => field.Or(ErrorCode);

	string? Core.IZodRule.Code => Code;

	string? Core.IZodRule.Origin => null;
}
