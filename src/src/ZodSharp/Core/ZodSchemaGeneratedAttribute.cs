namespace ZodSharp.Core;

/// <summary>
/// Marks a type as having a source-generated Zod schema validator, enabling auto-discovery by <see cref="IZodSchemaFactory"/>.
/// Applied at module level by the <c>ZodSchemaGenerator</c>.
/// </summary>
/// <remarks>Initializes a new instance.</remarks>
[AttributeUsage(
	AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Assembly,
	AllowMultiple = true,
	Inherited = false
)]
public sealed class ZodSchemaGeneratedAttribute(Type targetType) : Attribute
{
	/// <summary>The type that has a generated validator.</summary>
	public Type TargetType { get; } = targetType;
}
