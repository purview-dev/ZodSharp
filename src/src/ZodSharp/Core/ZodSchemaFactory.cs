using System.Collections.Concurrent;

namespace ZodSharp.Core;

/// <summary>
/// Thread-safe default <see cref="IZodSchemaFactory"/> backed by a concurrent dictionary keyed by validated type.
/// </summary>
public sealed class ZodSchemaFactory : IZodSchemaFactory
{
	readonly ConcurrentDictionary<Type, IZodSchemaValidator> _validators = new();

	/// <inheritdoc/>
	public IZodSchemaValidator<T>? Resolve<T>() =>
		_validators.TryGetValue(typeof(T), out var validator) ? (IZodSchemaValidator<T>)validator : null;

	/// <inheritdoc />
	public IZodSchemaValidator<T> ResolveRequired<T>() =>
		Resolve<T>()
		?? throw new InvalidOperationException($"No Zod schema validator registered for type '{typeof(T).FullName}'.");

	/// <inheritdoc/>
	public ValidationResult<T> Validate<T>(T value) => ResolveRequired<T>().Validate(value);

	/// <inheritdoc/>
	public void Register<T>(IZodSchemaValidator<T> validator) => _validators[typeof(T)] = validator;

	/// <inheritdoc/>
	public void Register(Type targetType, IZodSchemaValidator validator) => _validators[targetType] = validator;

	/// <inheritdoc/>
	public bool TryRegister<T>(IZodSchemaValidator<T> validator) => _validators.TryAdd(typeof(T), validator);

	/// <inheritdoc/>
	public bool IsRegistered<T>() => _validators.ContainsKey(typeof(T));
}
