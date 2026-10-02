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
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Min(long minValue)
	{
		AddRule(new MinValueRule<long>(minValue));
		return this;
	}

	/// <summary>
	/// Adds a maximum value validation.
	/// Equivalent to Zod's <c>z.bigint().max(value)</c>.
	/// </summary>
	/// <param name="maxValue">The maximum value (inclusive)</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Max(long maxValue)
	{
		AddRule(new MaxValueRule<long>(maxValue));
		return this;
	}

	/// <summary>
	/// Adds a strictly-greater-than validation.
	/// Equivalent to Zod's <c>z.bigint().gt(value)</c>.
	/// </summary>
	/// <param name="value">The exclusive lower bound</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Gt(long value)
	{
		AddRule(new GreaterThanRule<long>(value));
		return this;
	}

	/// <summary>
	/// Adds a greater-than-or-equal validation.
	/// Equivalent to Zod's <c>z.bigint().gte(value)</c>.
	/// </summary>
	/// <param name="value">The inclusive lower bound</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Gte(long value)
	{
		AddRule(new MinValueRule<long>(value));
		return this;
	}

	/// <summary>
	/// Adds a strictly-less-than validation.
	/// Equivalent to Zod's <c>z.bigint().lt(value)</c>.
	/// </summary>
	/// <param name="value">The exclusive upper bound</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Lt(long value)
	{
		AddRule(new LessThanRule<long>(value));
		return this;
	}

	/// <summary>
	/// Adds a less-than-or-equal validation.
	/// Equivalent to Zod's <c>z.bigint().lte(value)</c>.
	/// </summary>
	/// <param name="value">The inclusive upper bound</param>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Lte(long value)
	{
		AddRule(new MaxValueRule<long>(value));
		return this;
	}

	/// <summary>
	/// Adds a strictly positive validation (value must be greater than zero).
	/// Equivalent to Zod's <c>z.bigint().positive()</c>.
	/// </summary>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Positive()
	{
		AddRule(new GreaterThanRule<long>(0));
		return this;
	}

	/// <summary>
	/// Adds a strictly negative validation (value must be less than zero).
	/// Equivalent to Zod's <c>z.bigint().negative()</c>.
	/// </summary>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt Negative()
	{
		AddRule(new LessThanRule<long>(0));
		return this;
	}

	/// <summary>
	/// Adds a non-negative validation (value must be greater than or equal to zero).
	/// Equivalent to Zod's <c>z.bigint().nonnegative()</c>.
	/// </summary>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt NonNegative()
	{
		AddRule(new MinValueRule<long>(0));
		return this;
	}

	/// <summary>
	/// Adds a non-positive validation (value must be less than or equal to zero).
	/// Equivalent to Zod's <c>z.bigint().nonpositive()</c>.
	/// </summary>
	/// <returns>This schema for method chaining</returns>
	public ZodBigInt NonPositive()
	{
		AddRule(new MaxValueRule<long>(0));
		return this;
	}
}
