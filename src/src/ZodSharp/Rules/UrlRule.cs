using System.Text.RegularExpressions;

namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for URL format.
/// Uses struct to avoid allocations.
/// </summary>
public readonly struct UrlRule : Core.IValidationRule<string>
{
	static readonly Regex UrlRegex = new(
		@"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$",
		RegexOptions.Compiled | RegexOptions.IgnoreCase,
		TimeSpan.FromMilliseconds(100)
	);

	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the UrlRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public UrlRule(string? message = null)
	{
		_message = message;
	}

	/// <summary>
	/// Validates that the value is a valid URL.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return false;

		return UrlRegex.IsMatch(value)
			|| Uri.TryCreate(value, UriKind.Absolute, out var uri)
				&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value)
	{
		return _message ?? $"Invalid URL format: {value}";
	}
}
