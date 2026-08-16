namespace ZodSharp.Core;

/// <summary>
/// Base interface for all Zod schemas.
/// Provides type-safe validation with input and output types.
/// Similar to Zod's ZodType interface.
/// </summary>
/// <typeparam name="TOutput">The output type after validation</typeparam>
/// <typeparam name="TInput">The input type before validation (defaults to TOutput)</typeparam>
public interface IZodSchema<TOutput, TInput>
{
	/// <summary>
	/// Validates the input value and returns a validation result.
	/// Equivalent to Zod's safeParse method.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result containing the validated value or errors</returns>
	ValidationResult<TOutput> Validate(TInput value);

	/// <summary>
	/// Validates the input value asynchronously and returns a validation result.
	/// Equivalent to Zod's safeParseAsync method.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <param name="cancellationToken"></param>
	/// <returns>A task that completes with a validation result</returns>
	ValueTask<ValidationResult<TOutput>> ValidateAsync(TInput value, CancellationToken cancellationToken = default);
}

/// <summary>
/// Convenience interface for schemas where input and output are the same type.
/// </summary>
/// <typeparam name="T">The type</typeparam>
public interface IZodSchema<T> : IZodSchema<T, T> { }
