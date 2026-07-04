namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for string prefix.
/// Uses struct to avoid allocations.
/// </summary>
public readonly struct StartsWithRule : Core.IValidationRule<string>
{
	readonly string _prefix;
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the StartsWithRule struct.
	/// </summary>
	/// <param name="prefix">The required prefix</param>
	/// <param name="message">Optional error message</param>
	public StartsWithRule(string prefix, string? message = null)
	{
		_prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
		_message = message;
	}

	/// <summary>
	/// Validates that the value starts with the specified prefix.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value)
	{
		if (value == null)
			return false;

		return value.StartsWith(_prefix, StringComparison.Ordinal);
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value)
	{
		return _message ?? $"String must start with '{_prefix}', but got '{value}'";
	}
}
