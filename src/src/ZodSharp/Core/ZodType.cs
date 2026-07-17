using System.Collections.Immutable;

namespace ZodSharp.Core;

/// <summary>
/// Base class for all Zod schema types.
/// Provides common functionality and implements the fluent API pattern.
/// Similar to Zod's ZodType abstract class.
/// </summary>
/// <typeparam name="TOutput">The output type after validation</typeparam>
/// <typeparam name="TInput">The input type before validation</typeparam>
public abstract class ZodType<TOutput, TInput> : IZodSchema<TOutput, TInput>
{
	static readonly string[] EmptyPath = [];

	ImmutableArray<IValidationRule<TOutput>> _rules = [];

	/// <summary>
	/// Gets the description of this schema.
	/// </summary>
	public string? Description { get; private set; }

	/// <summary>
	/// Validates the input value and returns a validation result.
	/// Equivalent to Zod's safeParse method.
	/// </summary>
	public ValidationResult<TOutput> Validate(TInput value)
	{
		var parseResult = ParseInternal(value);
		if (!parseResult.IsSuccess)
			return parseResult;

		var validatedValue = parseResult.Value!;

		var rulesCount = _rules.Length;
		if (rulesCount == 0)
		{
			return ValidationResult<TOutput>.Success(validatedValue);
		}

		List<ValidationError>? errors = null;

		foreach (var rule in _rules)
		{
			if (!rule.IsValid(validatedValue))
			{
				errors ??= [with(rulesCount)];
				errors.Add(new ValidationError("validation_failed", rule.GetErrorMessage(validatedValue), EmptyPath));
			}
		}

		return errors != null && errors.Count > 0
			? ValidationResult<TOutput>.Failure(errors)
			: ValidationResult<TOutput>.Success(validatedValue);
	}

	/// <summary>
	/// Validates the input value asynchronously.
	/// Equivalent to Zod's safeParseAsync method.
	/// </summary>
	public ValueTask<ValidationResult<TOutput>> ValidateAsync(
		TInput value,
		CancellationToken cancellationToken = default
	) => new(Validate(value));

	/// <summary>
	/// Parses the input value to the output type.
	/// Override this method in derived classes to implement type-specific parsing.
	/// </summary>
	protected abstract ValidationResult<TOutput> ParseInternal(TInput value);

	/// <summary>
	/// Adds a validation rule to this schema.
	/// </summary>
	protected ZodType<TOutput, TInput> AddRule(IValidationRule<TOutput> rule)
	{
		_rules = _rules.Add(rule);
		return this;
	}

	/// <summary>
	/// Sets the description of this schema.
	/// Equivalent to Zod's describe method.
	/// </summary>
	public ZodType<TOutput, TInput> Describe(string description)
	{
		Description = description;
		return this;
	}

	/// <summary>
	/// Validates and returns the value, throwing an exception if validation fails.
	/// Equivalent to Zod's parse method.
	/// </summary>
	public TOutput Parse(TInput value) => Validate(value).GetValueOrThrow();

	/// <summary>
	/// Validates and returns a result without throwing.
	/// Equivalent to Zod's safeParse method.
	/// </summary>
	public ValidationResult<TOutput> SafeParse(TInput value) => Validate(value);

	/// <summary>
	/// Transforms the validated value using a function.
	/// Equivalent to Zod's transform method.
	/// Only works when TInput == TOutput (most common case).
	/// </summary>
	public Schemas.ZodTransform<TOutput, TNewOutput> Transform<TNewOutput>(Func<TOutput, TNewOutput> transform)
	{
		if (typeof(TInput) != typeof(TOutput))
		{
			throw new InvalidOperationException("Transform can only be used when input and output types are the same");
		}

		var adapter = (IZodSchema<TOutput, TOutput>)(object)this;
		return new Schemas.ZodTransform<TOutput, TNewOutput>(new RefinementAdapter<TOutput>(adapter), transform);
	}

	/// <summary>
	/// Adds a custom validation refinement.
	/// Equivalent to Zod's refine method.
	/// </summary>
	public Schemas.ZodRefinement<TOutput> Refine(Func<TOutput, bool> refinement, string? message = null)
	{
		if (typeof(TInput) != typeof(TOutput))
		{
			throw new InvalidOperationException("Refine can only be used when input and output types are the same");
		}

		var adapter = (IZodSchema<TOutput, TOutput>)(object)this;
		return new Schemas.ZodRefinement<TOutput>(new RefinementAdapter<TOutput>(adapter), refinement, message);
	}

	/// <summary>
	/// Adds a default value when input is null.
	/// Equivalent to Zod's default method.
	/// </summary>
	public Schemas.ZodDefault<TOutput> Default(TOutput defaultValue)
	{
		if (typeof(TInput) != typeof(TOutput))
		{
			throw new InvalidOperationException("Default can only be used when input and output types are the same");
		}

		var adapter = (IZodSchema<TOutput, TOutput>)(object)this;
		return new Schemas.ZodDefault<TOutput>(new RefinementAdapter<TOutput>(adapter), defaultValue);
	}

	sealed class TransformInputAdapter<TAdapterInput, TAdapterOutput>(IZodSchema<TAdapterOutput, TAdapterInput> inner)
		: IZodSchema<TAdapterOutput, TAdapterInput>
	{
		public ValidationResult<TAdapterOutput> Validate(TAdapterInput value) => inner.Validate(value);

		public ValueTask<ValidationResult<TAdapterOutput>> ValidateAsync(
			TAdapterInput value,
			CancellationToken cancellationToken = default
		) => inner.ValidateAsync(value, cancellationToken);
	}

	class RefinementAdapter<TAdapterType>(IZodSchema<TAdapterType, TAdapterType> inner) : IZodSchema<TAdapterType>
	{
		public ValidationResult<TAdapterType> Validate(TAdapterType value) => inner.Validate(value);

		public ValueTask<ValidationResult<TAdapterType>> ValidateAsync(
			TAdapterType value,
			CancellationToken cancellationToken = default
		) => inner.ValidateAsync(value, cancellationToken);
	}
}

/// <summary>
/// Convenience base class for schemas where input and output are the same type.
/// </summary>
/// <typeparam name="T">The type</typeparam>
public abstract class ZodType<T> : ZodType<T, T>, IZodSchema<T>, IZodSchemaValidator<T> { }
