using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ZodSharp.Core;

/// <summary>
/// Extension methods for registering validators discovered via <see cref="ZodSchemaGeneratedAttribute"/>.
/// </summary>
public static class ZodSchemaFactoryExtensions
{
	/// <summary>
	/// Scans <paramref name="assembly"/> for <see cref="ZodSchemaGeneratedAttribute"/> and registers
	/// a generated <c>{TypeName}SchemaValidator</c> instance for each target type.
	/// Generated validators are expected to live in the same namespace as the target type and be named
	/// <c>{TypeName}SchemaValidator</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Safe under trimming and Native AOT. The generator records the validator type in the attribute as a
	/// <c>typeof</c>, which roots it for the trimmer, and
	/// <see cref="ZodSchemaGeneratedAttribute.ValidatorType"/> is annotated
	/// <see cref="DynamicallyAccessedMemberTypes.PublicParameterlessConstructor"/> so its constructor is
	/// preserved as well. Nothing here resolves a type from a string.
	/// </para>
	/// <para>
	/// This reads attribute metadata rather than scanning types, so its cost is proportional to the number
	/// of generated schemas, not to the size of the assembly.
	/// </para>
	/// </remarks>
	public static IZodSchemaFactory RegisterFromAssembly(this IZodSchemaFactory factory, Assembly assembly)
	{
		if (factory is null)
			throw new ArgumentNullException(nameof(factory));
		if (assembly is null)
			throw new ArgumentNullException(nameof(assembly));

		foreach (var attr in assembly.GetCustomAttributes<ZodSchemaGeneratedAttribute>())
		{
			// Activator.CreateInstance over a type the attribute names directly is trim- and AOT-safe: the
			// type is rooted by the typeof in metadata, and its parameterless constructor is kept by the
			// DynamicallyAccessedMembers annotation on ValidatorType.
			if (Activator.CreateInstance(attr.ValidatorType) is not IZodSchemaValidator validator)
				throw new InvalidOperationException(
					$"Generated validator '{attr.ValidatorType.FullName}' does not implement IZodSchemaValidator."
				);

			factory.Register(attr.TargetType, validator);
		}

		return factory;
	}

	/// <summary>
	/// Scans <typeparamref name="T"/>'s assembly for <see cref="ZodSchemaGeneratedAttribute"/> and registers
	/// a generated <c>{TypeName}SchemaValidator</c> instance for each target type.
	/// Generated validators are expected to live in the same namespace as the target type and be named
	/// <c>{TypeName}SchemaValidator</c>.
	/// </summary>
	/// <remarks>
	/// Safe under trimming and Native AOT. See the other overload.
	/// </remarks>
	public static IZodSchemaFactory RegisterFromAssembly<T>(this IZodSchemaFactory factory) =>
		RegisterFromAssembly(factory, typeof(T).Assembly);
}
