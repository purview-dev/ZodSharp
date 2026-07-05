namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for a Uri.
/// <strong>This will allocate by utilising <see cref="Uri.TryCreate(string?, UriKind, out Uri?)"/></strong>
/// </summary>
public readonly record struct UriRule : Core.IValidationRule<string>
{
	readonly string? _message;
	readonly UriKind _uriKind;

	/// <summary>
	/// Initializes a new instance of the UrlRule struct.
	/// </summary>
	/// <param name="uriKind">The uri kind to validate against</param>
	/// <param name="message">Optional error message</param>
	public UriRule(UriKind uriKind, string? message = null)
	{
		_uriKind = uriKind;
		_message = message.OrNull();
	}

	/// <summary>
	/// Validates that the value is a valid URL.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) =>
		!string.IsNullOrWhiteSpace(value) && Uri.TryCreate(value, _uriKind, out var _);

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? $"Invalid Uri, kind: {_uriKind}), format: {value}";
}
