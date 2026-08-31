namespace ZodSharp.Core;

/// <summary>
/// Identifies schemas that can validate a null input value, such as
/// <see cref="Schemas.ZodNullable{T}"/> or <see cref="Schemas.ZodOptional{T}"/>.
/// Used by object-field wrappers to route null values to the inner schema instead
/// of failing coercion (which cannot represent null for non-nullable value types).
/// </summary>
public interface IAcceptsNull
{
	/// <summary>
	/// Validates a null input value, returning the result boxed as <see cref="object"/>.
	/// </summary>
	ValidationResult<object> ValidateNull();
}
