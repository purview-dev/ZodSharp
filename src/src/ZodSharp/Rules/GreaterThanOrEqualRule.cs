namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for an inclusive lower numeric bound (greater than or equal to).
/// Uses struct to avoid allocations.
/// </summary>
[Core.ZodRule]
public readonly record struct GreaterThanOrEqualRule : Core.IValidationRule<double>, Core.IZodRule
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "too_small";

	/// <summary>Gets the message format; <c>{0}</c> is the bound and <c>{1}</c> the offending value.</summary>
	public const string MessageFormat = "Value must be greater than or equal to {0}, but got {1}";

	readonly double _minValue;
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the GreaterThanOrEqualRule struct.
	/// </summary>
	/// <param name="minValue">The inclusive lower bound</param>
	/// <param name="message">Optional error message</param>
	public GreaterThanOrEqualRule(double minValue, string? message = null)
	{
		_minValue = minValue;
		_message = message.OrNull();
	}

	/// <summary>
	/// Validates that the value is greater than or equal to the configured bound.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in double value) => value >= _minValue;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in double value) => _message ?? RuleMessage.Format(MessageFormat, _minValue, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => ErrorCode;

	string? Core.IZodRule.Origin => null;
}
