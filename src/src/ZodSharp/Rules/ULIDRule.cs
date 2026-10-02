namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for ULID format: 26 Crockford base32 characters.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct ULIDRule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the ULIDRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public ULIDRule(string? message = null) => _message = message.OrNull();

	/// <summary>
	/// Validates that the value is a ULID.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value is not null && IsValid(value.AsSpan());

	/// <summary>
	/// Validates that the span is a ULID without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value)
	{
		if (value.Length != 26)
			return false;

		// The first character encodes the 48-bit timestamp, so it is always 0-7 (Zod parity).
		if (value[0] is < '0' or > '7')
			return false;

		foreach (var c in value)
		{
			if (!IsCrockfordBase32(c))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? $"Invalid ULID: {value}";

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) => _message ?? $"Invalid ULID: {value}";

	static bool IsCrockfordBase32(char c) =>
		c switch
		{
			>= '0' and <= '9' => true,
			>= 'A' and <= 'H' => true,
			>= 'J' and <= 'K' => true,
			>= 'M' and <= 'N' => true,
			>= 'P' and <= 'T' => true,
			>= 'V' and <= 'Z' => true,
			>= 'a' and <= 'h' => true,
			>= 'j' and <= 'k' => true,
			>= 'm' and <= 'n' => true,
			>= 'p' and <= 't' => true,
			>= 'v' and <= 'z' => true,
			_ => false,
		};

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => "invalid_string";
}
