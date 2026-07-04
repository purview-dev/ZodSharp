using System.Collections.Immutable;
using System.Reflection;
using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for object validation.
/// Validates object properties against their schemas.
/// </summary>
public class ZodObject : ZodType<Dictionary<string, object?>, Dictionary<string, object?>>
{
	readonly ImmutableDictionary<string, IZodSchema<object, object>> _shape;

	/// <summary>
	/// Initializes a new instance of the ZodObject class.
	/// </summary>
	/// <param name="shape">The object shape (property schemas)</param>
	public ZodObject(ImmutableDictionary<string, IZodSchema<object, object>> shape)
	{
		_shape = shape;
	}

	static readonly string[] EmptyPath = Array.Empty<string>();

	/// <summary>
	/// Parses and validates an object value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<Dictionary<string, object?>> ParseInternal(Dictionary<string, object?> value)
	{
		if (value == null)
		{
			return ValidationResult<Dictionary<string, object?>>.Failure(
				new ValidationError("invalid_type", "Expected object, but got null", EmptyPath)
			);
		}

		var shapeCount = _shape.Count;
		var errors = new List<ValidationError>(shapeCount);
		var validatedObject = new Dictionary<string, object?>(shapeCount);

		foreach (var (key, schema) in _shape)
		{
			if (!value.TryGetValue(key, out var propertyValue))
			{
				errors.Add(new ValidationError("missing_field", $"Required field '{key}' is missing", new[] { key }));
				continue;
			}

			var result = schema.Validate(propertyValue!);

			if (!result.IsSuccess)
			{
				foreach (var error in result.Errors)
				{
					var path = new string[error.Path.Length + 1];
					path[0] = key;
					Array.Copy(error.Path, 0, path, 1, error.Path.Length);
					errors.Add(new ValidationError(error.Code, error.Message, path, error.Parameters));
				}
			}
			else
			{
				validatedObject[key] = result.Value;
			}
		}

		if (errors.Count > 0)
		{
			return ValidationResult<Dictionary<string, object?>>.Failure(errors);
		}

		return ValidationResult<Dictionary<string, object?>>.Success(validatedObject);
	}
}

/// <summary>
/// Builder for creating object schemas with a fluent API.
/// </summary>
public class ZodObjectBuilder
{
	readonly Dictionary<string, IZodSchema<object, object>> _shape = new();

	/// <summary>
	/// Adds a field to the object schema.
	/// </summary>
	/// <typeparam name="T">The field type</typeparam>
	/// <param name="name">The field name</param>
	/// <param name="schema">The field schema</param>
	/// <returns>This builder for method chaining</returns>
	public ZodObjectBuilder Field<T>(string name, IZodSchema<T, T> schema)
	{
		_shape[name] = new SchemaWrapper<T>(schema);
		return this;
	}

	/// <summary>
	/// Builds the object schema.
	/// </summary>
	/// <returns>The built object schema</returns>
	public ZodObject Build()
	{
		return new ZodObject(_shape.ToImmutableDictionary());
	}

	/// <typeparam name="T">The inner type</typeparam>
	class SchemaWrapper<T> : IZodSchema<object, object>
	{
		readonly IZodSchema<T, T> _inner;

		public SchemaWrapper(IZodSchema<T, T> inner)
		{
			_inner = inner;
		}

		public ValidationResult<object> Validate(object value)
		{
			if (value is T typedValue)
			{
				var result = _inner.Validate(typedValue);
				if (result.IsSuccess)
				{
					return ValidationResult<object>.Success(result.Value!);
				}
				return ValidationResult<object>.Failure(result.Errors);
			}
			return ValidationResult<object>.Failure(
				new ValidationError(
					"invalid_type",
					$"Expected {typeof(T).Name}, but got {value?.GetType().Name ?? "null"}",
					Array.Empty<string>()
				)
			);
		}

		public ValueTask<ValidationResult<object>> ValidateAsync(object value)
		{
			return new ValueTask<ValidationResult<object>>(Validate(value));
		}
	}
}
