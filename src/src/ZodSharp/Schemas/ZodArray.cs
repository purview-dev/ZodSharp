using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for array validation.
/// </summary>
/// <typeparam name="T">The element type</typeparam>
/// <remarks>
/// Initializes a new instance of the ZodArray class.
/// </remarks>
/// <param name="elementSchema">The schema for array elements</param>
public class ZodArray<T>(IZodSchema<T, T> elementSchema) : ZodType<T[], T[]>, JsonSchema.IJsonSchemaArrayInfo
{
	static readonly string[] EmptyPath = [];

	int? _minLength;
	int? _maxLength;

	string? _errorMessage;

	// IJsonSchemaArrayInfo: the JSON Schema exporter's non-generic seam. It previously reflected on private
	// field names that do not exist, so bounds and the element schema were silently dropped from every
	// exported schema. See IJsonSchemaArrayInfo.
	int? JsonSchema.IJsonSchemaArrayInfo.MinItems => _minLength;

	int? JsonSchema.IJsonSchemaArrayInfo.MaxItems => _maxLength;

	object JsonSchema.IJsonSchemaArrayInfo.ElementSchema => elementSchema;

	/// <summary>
	/// Parses and validates an array value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<T[]> ParseInternal(T[] value)
	{
		if (value == null)
		{
			return ValidationResult<T[]>.Failure(
				new ValidationError("invalid_type", "Expected array, but got null", EmptyPath)
			);
		}

		// The length constraints depend only on the array's length, so they are checked before any element
		// is validated. Checking them afterwards meant an array of a million elements against .Max(10) ran a
		// million element validations and allocated a million ValidationErrors — each with an interpolated
		// index string — before reporting that the array was simply too long. A single request could pin a
		// core and a large heap allocation on input the schema had already declared out of range.
		//
		// This does change which error is reported when an array is both the wrong length and has invalid
		// elements: the length failure now wins. That is the more useful answer, and it matches the
		// container constraint being the outer one.
		if (LengthFailure(value.Length) is { } lengthFailure)
			return ValidationResult<T[]>.Failure(lengthFailure);

		List<ValidationError>? errors = null;
		List<T>? rebuilt = null;

		for (var i = 0; i < value.Length; i++)
		{
			var itemResult = elementSchema.Validate(value[i]);
			if (!itemResult.IsSuccess)
			{
				errors ??= [];
				foreach (var error in itemResult.Errors)
				{
					// The index is the prefix, and the element's own path follows it, so a nested
					// failure reads "[0].email". Use the 4-argument CopyTo: the 2-argument overload
					// takes a *destination* index, which would copy the element path to the end and
					// then have it overwritten by the index segment below.
					var path = new string[error.Path.Length + 1];
					path[0] = $"[{i}]";

					error.Path.CopyTo(0, path, 1, error.Path.Length);
					errors.Add(new(error.Code, error.Message, path, error.Parameters));
				}
			}
			else
			{
				var itemValue = itemResult.Value;
				if (rebuilt is null && !SameValue(itemValue, value[i]))
				{
					rebuilt = [with(value.Length)];
					for (var j = 0; j < i; j++)
						rebuilt.Add(value[j]);
				}

				rebuilt?.Add(itemValue);
			}
		}

		if (errors is { Count: > 0 })
			return ValidationResult<T[]>.Failure(errors);

		// When every element passed through unchanged, the input array is already
		// the validated result, so reuse it instead of copying.
		return rebuilt is not null ? ValidationResult<T[]>.Success([.. rebuilt]) : ValidationResult<T[]>.Success(value);
	}

	/// <summary>
	/// The length-constraint failure for <paramref name="count"/>, or <see langword="null"/> when the length
	/// is acceptable.
	/// </summary>
	ValidationError? LengthFailure(int count)
	{
		if (_minLength.HasValue && count < _minLength.Value)
		{
			return new ValidationError(
				"too_small",
				_errorMessage ?? $"Array must have at least {_minLength.Value} elements, but got {count}",
				EmptyPath,
				parameters: null,
				origin: "array",
				minimum: _minLength.Value,
				maximum: _maxLength,
				inclusive: true
			);
		}

		if (_maxLength.HasValue && count > _maxLength.Value)
		{
			return new ValidationError(
				"too_big",
				_errorMessage ?? $"Array must have at most {_maxLength.Value} elements, but got {count}",
				EmptyPath,
				parameters: null,
				origin: "array",
				minimum: _minLength,
				maximum: _maxLength.Value,
				inclusive: true
			);
		}

		// No length failure.
		return null;
	}

	/// <summary>
	/// Returns <see langword="true"/> when two element values are interchangeable,
	/// i.e. no transform produced a different value. Reference types compare by
	/// reference; value types by value (so a freshly boxed value still matches).
	/// </summary>
	static bool SameValue(T left, T right)
	{
		if (typeof(T).IsValueType)
			return EqualityComparer<T>.Default.Equals(left, right);

		// Reference types are interchangeable only when they are the same instance.
		return ReferenceEquals(left, right);
	}

	/// <summary>
	/// Sets the minimum array length.
	/// </summary>
	/// <param name="minLength">The minimum length</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodArray<T> Min(int minLength, string? message = null)
	{
		_minLength = minLength;
		_errorMessage = message.Or($"Array must have at least {minLength} elements");

		return this;
	}

	/// <summary>
	/// Sets the maximum array length.
	/// </summary>
	/// <param name="maxLength">The maximum length</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodArray<T> Max(int maxLength, string? message = null)
	{
		_maxLength = maxLength;
		_errorMessage = message.Or($"Array must have at most {maxLength} elements");

		return this;
	}

	/// <summary>
	/// Sets the exact array length.
	/// </summary>
	/// <param name="length">The exact length</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodArray<T> Length(int length, string? message = null)
	{
		_minLength = length;
		_maxLength = length;
		_errorMessage = message.Or($"Array must have exactly {length} elements");

		return this;
	}

	/// <summary>
	/// Requires the array to be non-empty (at least 1 element).
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodArray<T> NonEmpty(string? message = null)
	{
		_minLength = 1;
		_errorMessage = message.Or("Array must not be empty");

		return this;
	}
}
