using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema wrapper that makes a value nullable.
/// </summary>
/// <typeparam name="T">The inner schema type</typeparam>
/// <remarks>
/// Initializes a new instance of the ZodNullable class.
/// </remarks>
/// <param name="innerSchema">The inner schema</param>
public class ZodNullable<T>(IZodSchema<T, T> innerSchema)
	: ZodType<T?, T?>,
		IAcceptsNull,
		JsonSchema.IJsonSchemaNullableInfo
	where T : struct
{
	// The exporter previously reached the inner schema by reflecting a field named "_innerSchema", which does
	// not exist (it is a primary-constructor parameter), so a nullable schema exported with no inner type at
	// all. See IJsonSchemaNullableInfo.
	object JsonSchema.IJsonSchemaNullableInfo.NullableInnerSchema => innerSchema;

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

		var result = innerSchema.Validate(value.Value);
		return result.IsSuccess
			? ValidationResult<T?>.Success(result.Value)
			: ValidationResult<T?>.Failure(result.Errors);
	}
}
