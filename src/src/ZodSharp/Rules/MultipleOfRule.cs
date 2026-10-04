using System.Numerics;

namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for multiple-of check.
/// Uses struct to avoid allocations.
/// </summary>
/// <typeparam name="T">The numeric type; any <see cref="INumber{T}"/> is supported.</typeparam>
[Core.ZodRule]
public readonly record struct MultipleOfRule<T> : Core.IValidationRule<T>, Core.IZodRule
	where T : INumber<T>
{
	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public const string ErrorCode = "not_multiple_of";

	/// <summary>Gets the message format; <c>{0}</c> is the divisor and <c>{1}</c> the offending value.</summary>
	public const string MessageFormat = "Number must be a multiple of {0}, but got {1}";

	/// <summary>
	/// The relative tolerance applied when comparing the distance to the nearest multiple.
	/// </summary>
	const double RelativeTolerance = 1e-12;

	readonly T _divisor;
	readonly string _message;
	readonly string _code;

	/// <summary>
	/// Initializes a new instance of the MultipleOfRule struct.
	/// </summary>
	/// <param name="divisor">The divisor</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	public MultipleOfRule(T divisor, string? message = null, string? code = null)
	{
		if (divisor == T.Zero)
			throw new ArgumentException("Divisor cannot be zero", nameof(divisor));

		_divisor = divisor;
		_message = message.Or(MessageFormat);
		_code = code.Or(ErrorCode);
	}

	/// <summary>
	/// Validates that the value is a multiple of the divisor.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	/// <remarks>
	/// Floating-point division is inexact (for example <c>0.3 / 0.1 == 2.9999999999999996</c>), so the
	/// distance to the nearest multiple is compared against a relative tolerance instead of testing the raw
	/// remainder. The tolerance saturates to zero for types that cannot represent it (the integer types), so
	/// those compare exactly.
	/// </remarks>
	public bool IsValid(in T value)
	{
		if (!T.IsFinite(value))
			return false;

		var remainder = T.Abs(value % _divisor);
		var distance = T.Min(remainder, T.Abs(_divisor) - remainder);
		var tolerance = T.CreateSaturating(RelativeTolerance) * T.Max(T.Abs(_divisor), T.Abs(value));
		return distance <= tolerance;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in T value) => RuleMessage.Format(_message ?? MessageFormat, _divisor, value);

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => ErrorCode;

	string? Core.IZodRule.Code => _code;

	string? Core.IZodRule.Origin => null;
}
