using ZodSharp.Core;

namespace ZodSharp;

public static class ZExtensions
{
	/// <summary>
	/// Validates the specified value against the schema and throws an exception if validation fails.
	/// </summary>
	/// <typeparam name="T">The type of the value to validate.</typeparam>
	/// <param name="schema">The schema to validate against.</param>
	/// <param name="value">The value to validate.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the schema is null.</exception>
	public static async ValueTask ThrowOnErrorAsync<T>(
		this IZodSchemaValidator<T> schema,
		T value,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(schema);

		var result = await schema.ValidateAsync(value, cancellationToken);
		result.ThrowOnError();
	}

	/// <summary>
	/// Validates the specified value against the schema and returns the validated value or throws an exception if
	/// validation fails.
	/// </summary>
	/// <typeparam name="T">The type of the value to validate.</typeparam>
	/// <param name="schema">The schema to validate against.</param>
	/// <param name="value">The value to validate.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the schema is null.</exception>
	public static async ValueTask<T> GetOrThrowOnErrorAsync<T>(
		this IZodSchemaValidator<T> schema,
		T value,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(schema);

		var result = await schema.ValidateAsync(value, cancellationToken);
		return result.GetValueOrThrow();
	}
}
