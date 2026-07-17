namespace ZodSharp.Core;

/// <summary>
/// Non-generic marker for all Zod schema validators resolved by the DI factory.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1040:Avoid empty interfaces",
	Justification = "Marker interface for non-generic DI resolution."
)]
public interface IZodSchemaValidator { }

/// <summary>
/// Type-safe validator contract usable from DI. Mirrors <see cref="IZodSchema{TOutput, TInput}.Validate"/>.
/// </summary>
/// <typeparam name="T">The validated value type.</typeparam>
public interface IZodSchemaValidator<T> : IZodSchemaValidator
{
	/// <summary>
	/// Validates <paramref name="value"/> and returns a result.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <returns>A validation result containing the validated value or errors.</returns>
	ValidationResult<T> Validate(T value);

	/// <summary>
	/// Validates the input value asynchronously and returns a validation result.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <param name="cancellationToken"></param>
	/// <returns>A task that completes with a validation result.</returns>
	ValueTask<ValidationResult<T>> ValidateAsync(T value, CancellationToken cancellationToken = default);
}
