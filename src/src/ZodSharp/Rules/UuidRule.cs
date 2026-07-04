using System.Text.RegularExpressions;

namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for UUID format.
/// Uses struct to avoid allocations.
/// </summary>
public readonly struct UuidRule : Core.IValidationRule<string>
{
	static readonly Regex UuidRegex = new(
		@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
		RegexOptions.Compiled | RegexOptions.IgnoreCase,
		TimeSpan.FromMilliseconds(100)
	);

	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the UuidRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public UuidRule(string? message = null)
	{
		_message = message;
	}

	/// <summary>
	/// Validates that the value is a valid UUID.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return false;

		return UuidRegex.IsMatch(value);
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value)
	{
		return _message ?? $"Invalid UUID format: {value}";
	}
}
