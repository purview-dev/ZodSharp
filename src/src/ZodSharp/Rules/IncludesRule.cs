namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for string containment.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct IncludesRule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	readonly string _substring;
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the IncludesRule struct.
	/// </summary>
	/// <param name="substring">The required substring</param>
	/// <param name="message">Optional error message</param>
	public IncludesRule(string substring, string? message = null)
	{
		_substring = substring ?? throw new ArgumentNullException(nameof(substring));
		_message = message.OrNull();
	}

	/// <summary>
	/// Validates that the value contains the configured substring.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value is not null && value.Contains(_substring, StringComparison.Ordinal);

	/// <summary>
	/// Validates that the span contains the configured substring without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value) => value.Contains(_substring.AsSpan(), StringComparison.Ordinal);

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) =>
		_message ?? $"String must contain '{_substring}', but got '{value}'";

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) =>
		_message ?? $"String must contain '{_substring}', but got '{value}'";

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => "invalid_string";
}
