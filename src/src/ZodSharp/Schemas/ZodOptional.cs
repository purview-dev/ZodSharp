using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema wrapper that makes a value optional.
/// </summary>
/// <typeparam name="T">The inner schema type</typeparam>
/// <remarks>
/// Initializes a new instance of the ZodOptional class.
/// </remarks>
/// <param name="innerSchema">The inner schema</param>
public class ZodOptional<T>(IZodSchema<T, T> innerSchema)
	: ZodType<T?, T?>,
		IAcceptsNull,
		JsonSchema.IJsonSchemaInnerSchema
	where T : class
{
	// In JSON Schema, optionality is expressed by omitting the property from "required", not by changing its
	// type — so the exporter unwraps to the inner schema. It previously tried to reach it by reflecting a
	// field named "_innerSchema", which does not exist (it is a primary-constructor parameter), so every
	// optional property exported as an empty "any" schema. See IJsonSchemaInnerSchema.
	object JsonSchema.IJsonSchemaInnerSchema.InnerSchema => innerSchema;

	/// <inheritdoc/>
	public override bool IsOptional => true;

	/// <inheritdoc/>
	public ValidationResult<object> ValidateNull() => ValidationResult<object>.Success(null!);

	/// <summary>
	/// Parses and validates the value, allowing null.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<T?> ParseInternal(T? value)
	{
		if (value == null)
			return ValidationResult<T?>.Success(null);

		var result = innerSchema.Validate(value);
		return result.IsSuccess
			? ValidationResult<T?>.Success(result.Value)
			: ValidationResult<T?>.Failure(result.Errors);
	}
}
