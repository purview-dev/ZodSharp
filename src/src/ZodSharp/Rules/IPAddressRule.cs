using System.Net;
using System.Net.Sockets;

namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for IPv4 and IPv6 address format.
/// Uses struct to avoid allocations.
/// </summary>
public readonly record struct IPAddressRule : Core.IValidationRule<string>, Core.IStringValidationRule
{
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the IPAddressRule struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public IPAddressRule(string? message = null) => _message = message.OrNull();

	/// <summary>
	/// Validates that the value is an IPv4 or IPv6 address.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in string value) => value is not null && IsValid(value.AsSpan());

	/// <summary>
	/// Validates that the span is an IPv4 or IPv6 address without materialising a string.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(ReadOnlySpan<char> value)
	{
		if (value.IsEmpty || value.IsWhiteSpace())
			return false;

		// Use IPAddress.TryParse to validate the IP address format without throwing exceptions
		return IPAddress.TryParse(value, out var address)
			&& address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in string value) => _message ?? $"Invalid IP address: {value}";

	/// <summary>
	/// Gets the error message for a failed span validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(ReadOnlySpan<char> value) => _message ?? $"Invalid IP address: {value}";

	/// <summary>Gets the Zod-compatible error code reported when the rule fails.</summary>
	public string Code => "invalid_string";
}
