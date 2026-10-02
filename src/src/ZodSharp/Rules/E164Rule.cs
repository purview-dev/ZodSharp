namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for E.164 phone number format (<c>+</c> followed by 7-15 digits, first digit 1-9).
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct E164Rule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the E164Rule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public E164Rule(string? message = null) => _message = message.OrNull();

	/// <summary>
	/// Validates that the value is an E.164 phone number.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value is not null && IsValid(value.AsSpan());

	/// <summary>
	/// Validates that the span is an E.164 phone number without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value)
	{
		// E.164: '+' then 1-9 then 6-14 more digits (7-15 digits total).
		if (value.Length is < 8 or > 16)
			return false;

		if (value[0] is not '+' || value[1] is < '1' or > '9')
			return false;

		for (var i = 2; i < value.Length; i++)
		{
			if (!char.IsAsciiDigit(value[i]))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? $"Invalid E.164 phone number: {value}";

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) => _message ?? $"Invalid E.164 phone number: {value}";

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => "invalid_string";
}
