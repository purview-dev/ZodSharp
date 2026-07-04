using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema that adds custom validation logic (refinement).
/// Equivalent to Zod's refine method.
/// </summary>
/// <typeparam name="T">The type being validated</typeparam>
public class ZodRefinement<T> : ZodType<T>
{
	readonly IZodSchema<T> _baseSchema;
	readonly Func<T, bool> _refinement;
	readonly string? _message;

	/// <summary>
	/// Initializes a new instance of the ZodRefinement class.
	/// </summary>
	/// <param name="baseSchema">The base schema</param>
	/// <param name="refinement">The refinement function</param>
	/// <param name="message">Optional error message</param>
	public ZodRefinement(IZodSchema<T> baseSchema, Func<T, bool> refinement, string? message = null)
	{
		_baseSchema = baseSchema;
		_refinement = refinement;
		_message = message;
	}

	/// <summary>
	/// Parses and validates the value with refinement.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<T> ParseInternal(T value)
	{
		var baseResult = _baseSchema.Validate(value);
		if (!baseResult.IsSuccess)
		{
			return baseResult;
		}

		if (!_refinement(baseResult.Value!))
		{
			return ValidationResult<T>.Failure(
				new ValidationError("refinement_failed", _message ?? "Custom validation failed", Array.Empty<string>())
			);
		}

		return ValidationResult<T>.Success(baseResult.Value!);
	}
}
