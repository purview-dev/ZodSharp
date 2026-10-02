namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for CUID2 format (lowercase alphanumeric characters).
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct Cuid2Rule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the Cuid2Rule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public Cuid2Rule(string? message = null) => _message = message.OrNull();

	/// <summary>
	/// Validates that the value is a CUID2.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value is not null && IsValid(value.AsSpan());

	/// <summary>
	/// Validates that the span is a CUID2 without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value)
	{
		if (value.IsEmpty)
			return false;

		foreach (var c in value)
		{
			if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? $"Invalid CUID2: {value}";

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) => _message ?? $"Invalid CUID2: {value}";

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => "invalid_string";
}
