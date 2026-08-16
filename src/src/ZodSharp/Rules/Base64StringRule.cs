namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for Base64 strings.
/// Mirrors the behavior of System.ComponentModel.DataAnnotations.Base64StringAttribute.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct Base64StringRule : Core.IValidationRule<string>
{
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the Base64StringRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public Base64StringRule(string? message = null)
	{
		_message = message.OrNull();
	}

	/// <summary>
	/// Validates that the value is a valid Base64 string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return false;

		try
		{
			Convert.FromBase64String(value);
			return true;
		}
		catch (FormatException)
		{
			return false;
		}
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? $"Invalid Base64 string format: {value}";
}
