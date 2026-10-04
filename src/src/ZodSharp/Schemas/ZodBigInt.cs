using ZodSharp.Core;
using ZodSharp.Rules;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for 64-bit integer (bigint) validation.
/// Equivalent to Zod's <c>z.bigint()</c>.
/// </summary>
public class ZodBigInt : ZodType<long>
{
	/// <summary>
	/// Parses and validates a 64-bit integer value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<long> ParseInternal(long value) => ValidationResult<long>.Success(value);

	/// <summary>
	/// Adds a minimum value validation.
	/// Equivalent to Zod's <c>z.bigint().min(value)</c>.
	/// </summary>
	/// <param name="minValue">The minimum value (inclusive)</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Min(long minValue, string? message = null, string? code = null)
	{
		AddRule(new MinValueRule<long>(minValue, message, code));
		return this;
	}

	/// <summary>
	/// Adds a maximum value validation.
	/// Equivalent to Zod's <c>z.bigint().max(value)</c>.
	/// </summary>
	/// <param name="maxValue">The maximum value (inclusive)</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Max(long maxValue, string? message = null, string? code = null)
	{
		AddRule(new MaxValueRule<long>(maxValue, message, code));
		return this;
	}

	/// <summary>
	/// Adds a strictly-greater-than validation.
	/// Equivalent to Zod's <c>z.bigint().gt(value)</c>.
	/// </summary>
	/// <param name="value">The exclusive lower bound</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Gt(long value, string? message = null, string? code = null)
	{
		AddRule(new GreaterThanRule<long>(value, message, code));
		return this;
	}

	/// <summary>
	/// Adds a greater-than-or-equal validation.
	/// Equivalent to Zod's <c>z.bigint().gte(value)</c>.
	/// </summary>
	/// <param name="value">The inclusive lower bound</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Gte(long value, string? message = null, string? code = null)
	{
		AddRule(new MinValueRule<long>(value, message, code));
		return this;
	}

	/// <summary>
	/// Adds a strictly-less-than validation.
	/// Equivalent to Zod's <c>z.bigint().lt(value)</c>.
	/// </summary>
	/// <param name="value">The exclusive upper bound</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Lt(long value, string? message = null, string? code = null)
	{
		AddRule(new LessThanRule<long>(value, message, code));
		return this;
	}

	/// <summary>
	/// Adds a less-than-or-equal validation.
	/// Equivalent to Zod's <c>z.bigint().lte(value)</c>.
	/// </summary>
	/// <param name="value">The inclusive upper bound</param>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Lte(long value, string? message = null, string? code = null)
	{
		AddRule(new MaxValueRule<long>(value, message, code));
		return this;
	}

	/// <summary>
	/// Adds a strictly positive validation (value must be greater than zero).
	/// Equivalent to Zod's <c>z.bigint().positive()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Positive(string? message = null, string? code = null)
	{
		AddRule(new GreaterThanRule<long>(0, message, code));
		return this;
	}

	/// <summary>
	/// Adds a strictly negative validation (value must be less than zero).
	/// Equivalent to Zod's <c>z.bigint().negative()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Negative(string? message = null, string? code = null)
	{
		AddRule(new LessThanRule<long>(0, message, code));
		return this;
	}

	/// <summary>
	/// Adds a non-negative validation (value must be greater than or equal to zero).
	/// Equivalent to Zod's <c>z.bigint().nonnegative()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt NonNegative(string? message = null, string? code = null)
	{
		AddRule(new MinValueRule<long>(0, message, code));
		return this;
	}

	/// <summary>
	/// Adds a non-positive validation (value must be less than or equal to zero).
	/// Equivalent to Zod's <c>z.bigint().nonpositive()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <param name="code">Optional error code override</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt NonPositive(string? message = null, string? code = null)
	{
		AddRule(new MaxValueRule<long>(0, message, code));
		return this;
	}
}
