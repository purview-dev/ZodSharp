using System.Diagnostics.CodeAnalysis;

namespace ZodSharp.Core;

/// <summary>
/// Marks a type as having a source-generated Zod schema validator, enabling auto-discovery by <see cref="IZodSchemaFactory"/>.
/// Applied at module level by the <c>ZodSchemaGenerator</c>.
/// </summary>
/// <remarks>
/// <para>
/// The attribute names the validator type directly rather than leaving the caller to reconstruct it. That
/// is what makes discovery safe under trimming and Native AOT: a <c>typeof</c> in attribute metadata roots
/// the validator so the trimmer keeps it, and <see cref="ValidatorType"/> is annotated
/// <see cref="DynamicallyAccessedMemberTypes.PublicParameterlessConstructor"/> so its constructor survives
/// too. Discovery previously rebuilt the type's name as a string and asked
/// <c>Assembly.GetType(string)</c> for it, which a trimmer cannot follow — so the validators were removed
/// from a trimmed application and registration threw at runtime.
/// </para>
/// <para>Initializes a new instance.</para>
/// </remarks>
/// <param name="targetType">The type that has a generated validator.</param>
/// <param name="validatorType">
/// The generated validator type. It must implement <see cref="IZodSchemaValidator"/> and expose a public
/// parameterless constructor.
/// </param>
[AttributeUsage(
	AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Assembly,
	AllowMultiple = true,
	Inherited = false
)]
public sealed class ZodSchemaGeneratedAttribute(
	Type targetType,
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type validatorType
) : Attribute
{
	/// <summary>The type that has a generated validator.</summary>
	public Type TargetType { get; } = targetType ?? throw new ArgumentNullException(nameof(targetType));

	/// <summary>
	/// The generated validator type for <see cref="TargetType"/>.
	/// </summary>
	/// <remarks>
	/// Annotated so the trimmer preserves the parameterless constructor that
	/// <see cref="ZodSchemaFactoryExtensions.RegisterFromAssembly(IZodSchemaFactory, System.Reflection.Assembly)"/>
	/// calls.
	/// </remarks>
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public Type ValidatorType { get; } = validatorType ?? throw new ArgumentNullException(nameof(validatorType));
}
