using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System;

/// <inheritdoc/>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringExtensions
{
	/// <summary>
	/// Returns the string if it is not null or empty; otherwise, returns null.
	/// </summary>
	/// <param name="value">The string to check.</param>
	/// <returns>The original string if it is not null or empty; otherwise, null.</returns>
	public static string? OrNull(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

	/// <summary>
	/// Returns the string if it is not null or empty; otherwise, returns the specified default value.
	/// </summary>
	/// <param name="value">The string to check.</param>
	/// <param name="defaultValue">The default value to return if the string is null or empty.</param>
	/// <returns>The original string if it is not null or empty; otherwise, the default value.</returns>
	[return: NotNullIfNotNull(nameof(defaultValue))]
	public static string Or(this string? value, string? defaultValue = "<null>") =>
		string.IsNullOrWhiteSpace(value) ? (defaultValue ?? "") : value;

	/// <summary>
	/// Returns the length of the string if it is not null or empty; otherwise, returns 0.
	/// </summary>
	/// <param name="value">The string to check.</param>
	/// <returns>The length of the string if it is not null or empty; otherwise, 0.</returns>
	public static int LengthOrDefault(this string? value) => value == null ? 0 : value.Length;
}
