using System.Collections.Immutable;

namespace ZodSharp.Core;

/// <summary>
/// Represents the result of a validation operation.
/// Uses struct to avoid allocations.
/// </summary>
/// <typeparam name="T">The type of the validated value</typeparam>
public readonly struct ValidationResult<T>
{
	/// <summary>
	/// Indicates whether the validation was successful.
	/// </summary>
	public bool IsSuccess { get; }

	/// <summary>
	/// The validated value. Only valid when IsSuccess is true.
	/// </summary>
	public T? Value { get; }

	/// <summary>
	/// The validation errors. Only populated when IsSuccess is false.
	/// </summary>
	public ImmutableArray<ValidationError> Errors { get; }

	ValidationResult(bool isSuccess, T? value, ImmutableArray<ValidationError> errors)
	{
		IsSuccess = isSuccess;
		Value = value;
		Errors = errors;
	}

	/// <summary>
	/// Creates a successful validation result.
	/// </summary>
	public static ValidationResult<T> Success(T value) => new(true, value, ImmutableArray<ValidationError>.Empty);

	/// <summary>
	/// Creates a failed validation result with a single error.
	/// </summary>
	public static ValidationResult<T> Failure(ValidationError error) =>
		new(false, default, ImmutableArray.Create(error));

	/// <summary>
	/// Creates a failed validation result with multiple errors.
	/// </summary>
	public static ValidationResult<T> Failure(ImmutableArray<ValidationError> errors) => new(false, default, errors);

	/// <summary>
	/// Creates a failed validation result with multiple errors.
	/// </summary>
	public static ValidationResult<T> Failure(IEnumerable<ValidationError> errors) =>
		new(false, default, errors.ToImmutableArray());

	/// <summary>
	/// Throws a ZodException if validation failed.
	/// </summary>
	/// <returns>The validated value</returns>
	/// <exception cref="ZodException">Thrown when validation fails</exception>
	public T GetValueOrThrow()
	{
		if (!IsSuccess)
			throw new ZodException(Errors);
		return Value!;
	}
}
