namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for hexadecimal string format.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct HexRule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the HexRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public HexRule(string? message = null) => _message = message.OrNull();

	/// <summary>
	/// Validates that the value is a hexadecimal string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value is not null && IsValid(value.AsSpan());

	/// <summary>
	/// Validates that the span is a hexadecimal string without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value)
	{
		foreach (var c in value)
		{
			if (!char.IsAsciiHexDigit(c))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? $"Invalid hexadecimal string: {value}";

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) => _message ?? $"Invalid hexadecimal string: {value}";

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => "invalid_string";
}
