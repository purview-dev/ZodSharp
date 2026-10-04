using System.Numerics;

namespace ZodSharp.Rules;

/// <summary>
/// Validation rule that requires an even number (a multiple of two).
/// Uses struct to avoid allocations.
/// </summary>
/// <typeparam name="T">The numeric type; any <see cref="INumber{T}"/> is supported.</typeparam>
[Core.ZodRule]
public readonly record struct EvenRule<T> : Core.IValidationRule<T>, Core.IZodRule
	where T : INumber<T>
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_value";

	/// <summary>Gets the message format; <c>{0}</c> is the offending value.</summary>
	public const string MessageFormat = "Number must be even, but got {0}";

	readonly string _message;
	readonly string _code;

	/// <summary>
	/// Initializes a new instance of the EvenRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	public EvenRule(string? message = null, string? code = null)
	{
		_message = message.Or(MessageFormat);
		_code = code.Or(ErrorCode);
	}

	/// <summary>
	/// Validates that the value is even.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in T value) => value % (T.One + T.One) == T.Zero;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in T value) => RuleMessage.Format(_message ?? MessageFormat, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => _code;

	string? Core.IZodRule.Origin => null;
}
