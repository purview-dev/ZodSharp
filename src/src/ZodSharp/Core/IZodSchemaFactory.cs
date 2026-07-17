namespace ZodSharp.Core;

/// <summary>
/// Resolves <see cref="IZodSchemaValidator{T}"/> instances by validated type, for DI scenarios.
/// </summary>
public interface IZodSchemaFactory
{
	/// <summary>Resolves the validator registered for <typeparamref name="T"/>.</summary>
	IZodSchemaValidator<T>? Resolve<T>();

	/// <summary>Resolves the validator registered for <typeparamref name="T"/> and throws if its doesn't exist.</summary>
	IZodSchemaValidator<T> ResolveRequired<T>();

	/// <summary>Validates <paramref name="value"/> using the registered validator for <typeparamref name="T"/>.</summary>
	ValidationResult<T> Validate<T>(T value);

	/// <summary>Registers a validator for <typeparamref name="T"/>, overwriting any existing registration.</summary>
	void Register<T>(IZodSchemaValidator<T> validator);

	/// <summary>Registers a non-generic validator instance for <paramref name="targetType"/>, overwriting any existing registration.</summary>
	void Register(Type targetType, IZodSchemaValidator validator);

	/// <summary>Registers only if no validator is already registered; returns false otherwise.</summary>
	bool TryRegister<T>(IZodSchemaValidator<T> validator);

	/// <summary>True if a validator is registered for <typeparamref name="T"/>.</summary>
	bool IsRegistered<T>();
}
