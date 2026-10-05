using ZodSharp.Core;
using ZodSharp.Rules;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for date validation.
/// Equivalent to Zod's <c>z.date()</c>.
/// </summary>
public class ZodDate : ZodType<DateTime>
{
	/// <summary>
	/// Parses and validates a date value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<DateTime> ParseInternal(DateTime value) =>
		ValidationResult<DateTime>.Success(value);

	/// <summary>
	/// Adds a minimum date validation.
	/// Equivalent to Zod's <c>z.date().min(value)</c>.
	/// </summary>
	/// <param name="minValue">The earliest allowed date (inclusive)</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodDate Min(DateTime minValue, string? message = null, string? code = null)
	{
		AddRule(new MinValueRule<DateTime>(minValue, message, code));
		return this;
	}

	/// <summary>
	/// Adds a maximum date validation.
	/// Equivalent to Zod's <c>z.date().max(value)</c>.
	/// </summary>
	/// <param name="maxValue">The latest allowed date (inclusive)</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodDate Max(DateTime maxValue, string? message = null, string? code = null)
	{
		AddRule(new MaxValueRule<DateTime>(maxValue, message, code));
		return this;
	}

	/// <summary>
	/// Adds a range validation requiring the date to fall between a minimum and maximum.
	/// </summary>
	/// <param name="minimum">The lower bound</param>
	/// <param name="maximum">The upper bound</param>
	/// <param name="minimumIsExclusive">Whether the lower bound is exclusive</param>
	/// <param name="maximumIsExclusive">Whether the upper bound is exclusive</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodDate Range(
		DateTime minimum,
		DateTime maximum,
		bool minimumIsExclusive = false,
		bool maximumIsExclusive = false,
		string? message = null,
		string? code = null
	)
	{
		AddRule(new RangeRule<DateTime>(minimum, maximum, minimumIsExclusive, maximumIsExclusive, message, code));
		return this;
	}

	/// <summary>
	/// Adds a non-sentinel validation that rejects <see cref="DateTime.MinValue"/> and
	/// <see cref="DateTime.MaxValue"/>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public override ZodDate NonSentinel(string? message = null, string? code = null)
	{
		AddRule(new NonSentinelRule<DateTime>(message, code));
		return this;
	}
}
