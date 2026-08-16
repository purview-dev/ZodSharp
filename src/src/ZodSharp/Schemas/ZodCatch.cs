using System.Collections.Immutable;
using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema that returns a fallback value when validation fails, instead of
/// propagating errors. Equivalent to Zod's <c>.catch(fallback)</c> method.
/// </summary>
/// <typeparam name="T">The validated type.</typeparam>
public class ZodCatch<T> : ZodType<T>
{
	readonly IZodSchema<T, T> _innerSchema;
	readonly T _fallback = default!;
	readonly Func<T, ImmutableArray<ValidationError>, T>? _fallbackFactory;

	/// <summary>
	/// Initializes a new instance of the <see cref="ZodCatch{T}"/> class with a
	/// constant fallback value.
	/// </summary>
	/// <param name="innerSchema">The inner schema.</param>
	/// <param name="fallback">The value returned when validation fails.</param>
	public ZodCatch(IZodSchema<T, T> innerSchema, T fallback)
	{
		_innerSchema = innerSchema;
		_fallback = fallback;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZodCatch{T}"/> class with a
	/// fallback computed from the validation context.
	/// </summary>
	/// <param name="innerSchema">The inner schema.</param>
	/// <param name="fallbackFactory">A function producing the fallback value from the input and the errors.</param>
	public ZodCatch(IZodSchema<T, T> innerSchema, Func<T, ImmutableArray<ValidationError>, T> fallbackFactory)
	{
		_innerSchema = innerSchema;
		_fallbackFactory = fallbackFactory;
	}

	/// <summary>
	/// Validates the value, returning the fallback when validation fails.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <returns>A successful result, either with the validated or the fallback value.</returns>
	protected override ValidationResult<T> ParseInternal(T value)
	{
		var result = _innerSchema.Validate(value);
		if (result.IsSuccess)
			return result;

		var caught = _fallbackFactory is null ? _fallback : _fallbackFactory(value, result.Errors);
		return ValidationResult<T>.Success(caught);
	}
}
