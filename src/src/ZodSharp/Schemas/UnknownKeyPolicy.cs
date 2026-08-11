namespace ZodSharp.Schemas;

/// <summary>
/// Defines how a <see cref="ZodObject"/> handles keys that are not in its shape.
/// </summary>
public enum UnknownKeyPolicy
{
	/// <summary>
	/// Silently drops unknown keys from the validated output (Zod's default).
	/// </summary>
	Strip,

	/// <summary>
	/// Keeps unknown keys in the validated output without validation.
	/// </summary>
	Passthrough,

	/// <summary>
	/// Rejects unknown keys with a validation error.
	/// </summary>
	Strict,
}
