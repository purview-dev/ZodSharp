namespace ZodSharp.Core;

/// <summary>
/// Wraps an <see cref="IZodSchema{TOutput, TInput}"/> as an <see cref="IZodSchemaValidator{T}"/> for DI registration.
/// </summary>
public sealed class ZodSchemaValidator<T>(IZodSchema<T, T> schema) : IZodSchemaValidator<T>
{
	readonly IZodSchema<T, T> _schema = schema;

	/// <inheritdoc/>
	public ValidationResult<T> Validate(T value) => _schema.Validate(value);

	/// <inheritdoc/>
	public ValueTask<ValidationResult<T>> ValidateAsync(T value, CancellationToken cancellationToken = default) =>
		_schema.ValidateAsync(value, cancellationToken);
}
