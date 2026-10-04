using Microsoft.Extensions.Options;
using ZodSharp.Core;

namespace ZodSharp.Options;

/// <summary>
/// A generic <see cref="IValidateOptions{T}"/> adapter that resolves the source-generated schema
/// validator for <typeparamref name="T"/> through <see cref="IZodSchemaFactory"/> at runtime,
/// allowing a single hand-written class to validate any registered options type.
/// </summary>
/// <typeparam name="T">The options type to validate.</typeparam>
/// <remarks>Initializes a new instance with the schema factory used to resolve validators.</remarks>
/// <param name="factory">The schema factory.</param>
/// <param name="missingValidatorBehavior">How to behave when no validator is registered for <typeparamref name="T"/>.</param>
public sealed class ZodSchemaOptionsValidator<T>(
	IZodSchemaFactory factory,
	MissingValidatorBehavior missingValidatorBehavior = MissingValidatorBehavior.Throw
) : IValidateOptions<T>
	where T : class
{
	/// <inheritdoc/>
	public ValidateOptionsResult Validate(string? name, T options)
	{
		var validator =
			missingValidatorBehavior == MissingValidatorBehavior.Throw
				? factory.ResolveRequired<T>()
				: factory.Resolve<T>();

		if (validator is null)
			return ValidateOptionsResult.Success;

		var result = validator.Validate(options);
		return result.IsSuccess
			? ValidateOptionsResult.Success
			: ValidateOptionsResult.Fail(result.Errors.Select(static error => error.Message));
	}
}
