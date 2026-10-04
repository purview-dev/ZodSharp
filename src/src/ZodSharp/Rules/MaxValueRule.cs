namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for maximum numeric value.
/// Uses struct to avoid allocations.
/// </summary>
/// <typeparam name="T">The numeric type</typeparam>
[Core.ZodRule]
public readonly record struct MaxValueRule<T> : Core.IValidationRule<T>, Core.IZodRule
	where T : IComparable<T>
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "too_big";

	/// <summary>Gets the message format; <c>{0}</c> is the bound and <c>{1}</c> the offending value.</summary>
	public const string MessageFormat = "Value must be at most {0}, but got {1}";

	readonly T _maxValue;
	readonly string _message;
	readonly string _code;

	/// <summary>
	/// Initializes a new instance of the MaxValueRule struct.
	/// </summary>
	/// <param name="maxValue">The maximum value</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	public MaxValueRule(T maxValue, string? message = null, string? code = null)
	{
		_maxValue = maxValue;
		_message = message.Or(MessageFormat);
		_code = code.Or(ErrorCode);
	}

	/// <summary>
	/// Validates that the value is less than or equal to the maximum value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in T value) => value.CompareTo(_maxValue) <= 0;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in T value) => RuleMessage.Format(_message ?? MessageFormat, _maxValue, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => _code;

	string? Core.IZodRule.Origin => null;
}
