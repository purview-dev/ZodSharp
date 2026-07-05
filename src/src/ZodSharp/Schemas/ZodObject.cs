using System.Collections.Immutable;
using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for object validation.
/// Validates object properties against their schemas.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ZodObject class.
/// </remarks>
/// <param name="shape">The object shape (property schemas)</param>
public class ZodObject(ImmutableDictionary<string, IZodSchema<object, object>> shape)
	: ZodType<Dictionary<string, object?>, Dictionary<string, object?>>
{
	static readonly string[] EmptyPath = [];

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

		var shapeCount = shape.Count;
		var errors = new List<ValidationError>(shapeCount);
		var validatedObject = new Dictionary<string, object?>(shapeCount);

		foreach (var (key, schema) in shape)
		{
			if (!value.TryGetValue(key, out var propertyValue))
			{
				errors.Add(new ValidationError("missing_field", $"Required field '{key}' is missing", [key]));
				continue;
			}

			var result = schema.Validate(propertyValue!);

			if (!result.IsSuccess)
			{
				foreach (var error in result.Errors)
				{
					var path = new string[error.Path.Length + 1];
					path[0] = key;

					error.Path.CopyTo(0, path, 1, error.Path.Length);
					errors.Add(new(error.Code, error.Message, path, error.Parameters));
				}
			}
			else
			{
				validatedObject[key] = result.Value;
			}
		}

		return errors.Count > 0
			? ValidationResult<Dictionary<string, object?>>.Failure(errors)
			: ValidationResult<Dictionary<string, object?>>.Success(validatedObject);
	}
}
