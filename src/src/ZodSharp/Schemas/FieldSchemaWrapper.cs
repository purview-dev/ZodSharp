using System.Diagnostics.CodeAnalysis;
using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Wraps a typed <see cref="IZodSchema{T, T}"/> into an <see cref="IZodSchema{Object, Object}"/>
/// by coercing the untyped <see cref="object"/> input via <see cref="SchemaValueCoercion"/>.
/// Used by <see cref="ZodObject.Extend{T}(string, IZodSchema{T, T})"/> and the object/union builders.
/// </summary>
/// <typeparam name="T">The inner schema's type.</typeparam>
public sealed class FieldSchemaWrapper<T>(IZodSchema<T, T> inner)
	: IZodSchema<object, object>,
		IOptionalSchema,
		JsonSchema.IJsonSchemaInnerSchema
{
	// The wrapper only adapts the input type; it does not change the exported JSON Schema shape. Exposing
	// the inner schema lets the exporter unwrap to the schema that does. Without this, every wrapped field
	// hit the exporter's generic fallback and exported with no type.
	object JsonSchema.IJsonSchemaInnerSchema.InnerSchema => inner;

	/// <inheritdoc/>
	public bool IsOptional => inner is IOptionalSchema o && o.IsOptional;

	/// <inheritdoc/>
	public bool ProvidesValueOnMissing => inner is IOptionalSchema o && o.ProvidesValueOnMissing;

	/// <summary>Wraps a typed schema into an untyped schema.</summary>
	[SuppressMessage(
		"Design",
		"CA1000",
		Justification = "Factory method is intentionally placed on the generic wrapper type."
	)]
	public static IZodSchema<object, object> Wrap(IZodSchema<T, T> schema) => new FieldSchemaWrapper<T>(schema);

	/// <inheritdoc/>
	public ValidationResult<object> Validate(object value) => SchemaValueCoercion.ValidateWrapped(inner, value);

	/// <inheritdoc/>
	public ValueTask<ValidationResult<object>> ValidateAsync(
		object value,
		CancellationToken cancellationToken = default
	) => new(Validate(value));
}
