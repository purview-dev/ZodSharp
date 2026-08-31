namespace ZodSharp.Core;

/// <summary>
/// Identifies schemas that accept missing input (e.g. a missing object key),
/// mirroring Zod's behavior where a field is optional when its schema accepts
/// <c>undefined</c>.
/// </summary>
public interface IOptionalSchema
{
	/// <summary>
	/// Gets whether the schema accepts a missing value. For example
	/// <see cref="Schemas.ZodOptional{T}"/> returns <see langword="true"/>.
	/// </summary>
	bool IsOptional { get; }

	/// <summary>
	/// Gets whether the schema substitutes a value when the input is missing.
	/// For example <see cref="Schemas.ZodDefault{T}"/> returns
	/// <see langword="true"/> because the default value is used.
	/// </summary>
	bool ProvidesValueOnMissing { get; }
}
