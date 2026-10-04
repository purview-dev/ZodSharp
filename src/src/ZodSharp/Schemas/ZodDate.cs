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
	/// <returns>This schema for method chaining</returns>
	public ZodDate Min(DateTime minValue)
	{
		AddRule(new MinValueRule<DateTime>(minValue));
		return this;
	}

	/// <summary>
	/// Adds a maximum date validation.
	/// Equivalent to Zod's <c>z.date().max(value)</c>.
	/// </summary>
	/// <param name="maxValue">The latest allowed date (inclusive)</param>
	/// <returns>This schema for method chaining</returns>
	public ZodDate Max(DateTime maxValue)
	{
		AddRule(new MaxValueRule<DateTime>(maxValue));
		return this;
	}

	/// <summary>
	/// Adds a non-sentinel validation that rejects <see cref="DateTime.MinValue"/> and
	/// <see cref="DateTime.MaxValue"/>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public override ZodDate NonSentinel(string? message = null)
	{
		AddRule(new NonSentinelRule<DateTime>(message));
		return this;
	}
}
