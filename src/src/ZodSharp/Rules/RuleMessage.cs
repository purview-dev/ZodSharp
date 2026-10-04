using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace ZodSharp.Rules;

/// <summary>
/// Formats rule error messages from the public message-format constants exposed by each rule, caching the
/// parsed <see cref="CompositeFormat"/> so a format string is compiled once rather than on every failure.
/// </summary>
/// <remarks>
/// Centralising formatting keeps the culture provider in one place (satisfying CA1305) and lets every rule
/// expose its message as a <c>public const string</c> without re-parsing that constant on each failure.
/// </remarks>
static class RuleMessage
{
	static readonly ConcurrentDictionary<string, CompositeFormat> Formats = new(StringComparer.Ordinal);

	/// <summary>Formats a rule message with a single argument.</summary>
	/// <typeparam name="TArg0">The argument type.</typeparam>
	/// <param name="format">The message format (a rule's <c>MessageFormat</c> constant).</param>
	/// <param name="arg0">The format argument.</param>
	/// <returns>The formatted message using <see cref="CultureInfo.CurrentCulture"/>.</returns>
	public static string Format<TArg0>(string format, TArg0 arg0) =>
		string.Format(CultureInfo.CurrentCulture, GetFormat(format), arg0);

	/// <summary>Formats a rule message with two arguments.</summary>
	/// <typeparam name="TArg0">The first argument type.</typeparam>
	/// <typeparam name="TArg1">The second argument type.</typeparam>
	/// <param name="format">The message format (a rule's <c>MessageFormat</c> constant).</param>
	/// <param name="arg0">The first format argument.</param>
	/// <param name="arg1">The second format argument.</param>
	/// <returns>The formatted message using <see cref="CultureInfo.CurrentCulture"/>.</returns>
	public static string Format<TArg0, TArg1>(string format, TArg0 arg0, TArg1 arg1) =>
		string.Format(CultureInfo.CurrentCulture, GetFormat(format), arg0, arg1);

	static CompositeFormat GetFormat(string format) => Formats.GetOrAdd(format, CompositeFormat.Parse);
}
