namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for an inclusive lower numeric bound (greater than or equal to).
/// Uses struct to avoid allocations.
/// </summary>
/// <typeparam name="T">The comparable type</typeparam>
[Core.ZodRule]
public readonly record struct GreaterThanOrEqualRule<T> : Core.IValidationRule<T>, Core.IZodRule
	where T : IComparable<T>
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "too_small";

	/// <summary>Gets the message format; <c>{0}</c> is the bound and <c>{1}</c> the offending value.</summary>
	public const string MessageFormat = "Value must be greater than or equal to {0}, but got {1}";

	readonly T _minValue;
	readonly string _message;

	/// <summary>
	/// Initializes a new instance of the GreaterThanOrEqualRule struct.
	/// </summary>
	/// <param name="minValue">The inclusive lower bound</param>
	/// <param name="message">Optional error message/ message format.</param>
	/// <param name="code">Optional error code override. If one is not specified then the <see cref="ErrorCode"/> is used.</param>
	public GreaterThanOrEqualRule(T minValue, string? message = null, string? code = null)
	{
		_minValue = minValue;
		_message = message.Or(MessageFormat);
		Code = code.Or(ErrorCode);
	}

	/// <summary>
	/// Validates that the value is greater than or equal to the configured bound.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in T value) => value.CompareTo(_minValue) >= 0;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in T value) => RuleMessage.Format(_message ?? MessageFormat, _minValue, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => field.Or(ErrorCode);

	string? Core.IZodRule.Code => Code;

	string? Core.IZodRule.Origin => null;
}
