using System.Runtime.CompilerServices;

namespace ZodSharp.Rules;

/// <summary>
/// Validation rule that rejects "sentinel" values — the framework default or boundary values that are
/// commonly persisted by an ORM to represent an unset field (for example <see cref="Guid.Empty"/> or
/// <see cref="DateTime.MinValue"/>/<see cref="DateTime.MaxValue"/>).
/// </summary>
/// <typeparam name="T">The value type; only the supported sentinel types are inspected.</typeparam>
/// <remarks>
/// <para>
/// Built-in detection covers the types that map to database columns with column-level sentinels:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Guid"/> — <see cref="Guid.Empty"/>.</description></item>
/// <item><description><see cref="DateTime"/> — <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/>.</description></item>
/// <item><description><see cref="DateTimeOffset"/> — <see cref="DateTimeOffset.MinValue"/> and <see cref="DateTimeOffset.MaxValue"/>.</description></item>
/// <item><description><see cref="DateOnly"/> — <see cref="DateOnly.MinValue"/> and <see cref="DateOnly.MaxValue"/>.</description></item>
/// <item><description><see cref="TimeOnly"/> — <see cref="TimeOnly.MinValue"/> and <see cref="TimeOnly.MaxValue"/>.</description></item>
/// <item><description><see cref="string"/> — <see langword="null"/>, empty, or whitespace.</description></item>
/// </list>
/// <para>
/// Every other type is treated as valid, so the rule can be closed with a type it does not know about
/// without rejecting that type's entire value range. Detection uses a <c>typeof(T)</c> dispatch and
/// reinterprets the value in place, so no boxing occurs on the validation path.
/// </para>
/// </remarks>
public readonly record struct NonSentinelRule<T> : Core.IValidationRule<T>
{
	/// <summary>Gets the error code reported when the rule fails.</summary>
	public const string ErrorCode = "invalid_value";

	/// <summary>Gets the message format; <c>{0}</c> is the offending value.</summary>
	public const string MessageFormat = "Value is a sentinel value, but got {0}";

	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the <see cref="NonSentinelRule{T}"/> struct.
	/// </summary>
	/// <param name="message">Optional error message</param>
	public NonSentinelRule(string? message = null) => _message = message.OrNull();

	/// <summary>
	/// Validates that the value is not a sentinel value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in T value) => !SentinelValues<T>.IsSentinel(value);

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in T value) => _message ?? RuleMessage.Format(MessageFormat, value);

	/// <summary>Gets the error code reported when the rule fails.</summary>
	public string Code => ErrorCode;
}

/// <summary>
/// Detects the sentinel value(s) for a supported type without boxing. Types that are not known to have a
/// sentinel are treated as never sentinel.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
static class SentinelValues<T>
{
	/// <summary>
	/// Determines whether <paramref name="value"/> is a sentinel value for <typeparamref name="T"/>.
	/// </summary>
	/// <param name="value">The value to inspect.</param>
	/// <returns><see langword="true"/> when the value is a known sentinel; otherwise <see langword="false"/>.</returns>
	public static bool IsSentinel(in T value)
	{
		if (typeof(T) == typeof(Guid))
			return Unsafe.As<T, Guid>(ref Unsafe.AsRef(in value)) == Guid.Empty;

		if (typeof(T) == typeof(DateTime))
		{
			var typed = Unsafe.As<T, DateTime>(ref Unsafe.AsRef(in value));
			return typed == DateTime.MinValue || typed == DateTime.MaxValue;
		}

		if (typeof(T) == typeof(DateTimeOffset))
		{
			var typed = Unsafe.As<T, DateTimeOffset>(ref Unsafe.AsRef(in value));
			return typed == DateTimeOffset.MinValue || typed == DateTimeOffset.MaxValue;
		}

		if (typeof(T) == typeof(DateOnly))
		{
			var typed = Unsafe.As<T, DateOnly>(ref Unsafe.AsRef(in value));
			return typed == DateOnly.MinValue || typed == DateOnly.MaxValue;
		}

		if (typeof(T) == typeof(TimeOnly))
		{
			var typed = Unsafe.As<T, TimeOnly>(ref Unsafe.AsRef(in value));
			return typed == TimeOnly.MinValue || typed == TimeOnly.MaxValue;
		}

		if (typeof(T) == typeof(string))
		{
			var typed = Unsafe.As<T, string?>(ref Unsafe.AsRef(in value));
			return string.IsNullOrWhiteSpace(typed);
		}

		return false;
	}
}
