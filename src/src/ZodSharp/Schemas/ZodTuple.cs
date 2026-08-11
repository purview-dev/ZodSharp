using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for typed tuple validation. Validates a two-element array where each
/// position is validated against its own schema, returning a
/// <see cref="ValueTuple{T1,T2}"/>. Equivalent to Zod's
/// <c>z.tuple([a, b])</c>.
/// </summary>
/// <typeparam name="T1">The first element type.</typeparam>
/// <typeparam name="T2">The second element type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ZodTuple{T1,T2}"/> class.
/// </remarks>
/// <param name="schema1">The schema for the first element.</param>
/// <param name="schema2">The schema for the second element.</param>
public class ZodTuple<T1, T2>(IZodSchema<T1, T1> schema1, IZodSchema<T2, T2> schema2)
	: ZodType<(T1, T2), object?[]>
{
	static readonly string[] EmptyPath = [];

	/// <summary>
	/// Validates a two-element array, returning a typed tuple.
	/// </summary>
	/// <param name="value">The array to validate.</param>
	/// <returns>A validation result containing a typed tuple.</returns>
	protected override ValidationResult<(T1, T2)> ParseInternal(object?[] value)
	{
		if (value is null)
		{
			return ValidationResult<(T1, T2)>.Failure(
				new ValidationError("invalid_type", "Expected tuple, but got null", EmptyPath)
			);
		}

		if (value.Length != 2)
		{
			return ValidationResult<(T1, T2)>.Failure(
				new ValidationError(
					"invalid_tuple_length",
					$"Expected tuple of length 2, but got {value.Length}",
					EmptyPath
				)
			);
		}

		List<ValidationError> errors = [];
		T1? v1 = default;
		T2? v2 = default;

		if (value[0] is T1 t1)
		{
			var r = schema1.Validate(t1);
			if (!r.IsSuccess)
				errors.AddRange(r.Errors);
			else
				v1 = r.Value;
		}
		else
		{
			errors.Add(new("invalid_type", $"Expected {typeof(T1).Name} at index 0", ["[0]"]));
		}

		if (value[1] is T2 t2)
		{
			var r = schema2.Validate(t2);
			if (!r.IsSuccess)
				errors.AddRange(r.Errors);
			else
				v2 = r.Value;
		}
		else
		{
			errors.Add(new("invalid_type", $"Expected {typeof(T2).Name} at index 1", ["[1]"]));
		}

		return errors.Count > 0
			? ValidationResult<(T1, T2)>.Failure(errors)
			: ValidationResult<(T1, T2)>.Success((v1!, v2!));
	}
}

/// <summary>
/// Schema for typed tuple validation of three elements. Equivalent to Zod's
/// <c>z.tuple([a, b, c])</c>.
/// </summary>
/// <typeparam name="T1">The first element type.</typeparam>
/// <typeparam name="T2">The second element type.</typeparam>
/// <typeparam name="T3">The third element type.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1005:Avoid excessive parameters on generic types"
)]
public class ZodTuple<T1, T2, T3>(
	IZodSchema<T1, T1> schema1,
	IZodSchema<T2, T2> schema2,
	IZodSchema<T3, T3> schema3
) : ZodType<(T1, T2, T3), object?[]>
{
	static readonly string[] EmptyPath = [];

	/// <summary>
	/// Validates a three-element array, returning a typed tuple.
	/// </summary>
	/// <param name="value">The array to validate.</param>
	/// <returns>A validation result containing a typed tuple.</returns>
	protected override ValidationResult<(T1, T2, T3)> ParseInternal(object?[] value)
	{
		if (value is null)
		{
			return ValidationResult<(T1, T2, T3)>.Failure(
				new ValidationError("invalid_type", "Expected tuple, but got null", EmptyPath)
			);
		}

		if (value.Length != 3)
		{
			return ValidationResult<(T1, T2, T3)>.Failure(
				new ValidationError(
					"invalid_tuple_length",
					$"Expected tuple of length 3, but got {value.Length}",
					EmptyPath
				)
			);
		}

		List<ValidationError> errors = [];
		T1? v1 = default;
		T2? v2 = default;
		T3? v3 = default;

		if (value[0] is T1 t1)
		{
			var r = schema1.Validate(t1);
			if (!r.IsSuccess)
				errors.AddRange(r.Errors);
			else
				v1 = r.Value;
		}
		else
		{
			errors.Add(new("invalid_type", $"Expected {typeof(T1).Name} at index 0", ["[0]"]));
		}

		if (value[1] is T2 t2)
		{
			var r = schema2.Validate(t2);
			if (!r.IsSuccess)
				errors.AddRange(r.Errors);
			else
				v2 = r.Value;
		}
		else
		{
			errors.Add(new("invalid_type", $"Expected {typeof(T2).Name} at index 1", ["[1]"]));
		}

		if (value[2] is T3 t3)
		{
			var r = schema3.Validate(t3);
			if (!r.IsSuccess)
				errors.AddRange(r.Errors);
			else
				v3 = r.Value;
		}
		else
		{
			errors.Add(new("invalid_type", $"Expected {typeof(T3).Name} at index 2", ["[2]"]));
		}

		return errors.Count > 0
			? ValidationResult<(T1, T2, T3)>.Failure(errors)
			: ValidationResult<(T1, T2, T3)>.Success((v1!, v2!, v3!));
	}
}
