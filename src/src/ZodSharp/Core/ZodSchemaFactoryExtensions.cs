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
	public static IZodSchemaFactory RegisterFromAssembly(
		this IZodSchemaFactory factory,
		Assembly assembly
	)
	{
		if (factory is null)
			throw new ArgumentNullException(nameof(factory));
		if (assembly is null)
			throw new ArgumentNullException(nameof(assembly));

		foreach (var attr in assembly.GetCustomAttributes<ZodSchemaGeneratedAttribute>())
		{
			var targetType = attr.TargetType;
			var validatorTypeName = $"{targetType.Name}SchemaValidator";
			var validatorType =
				targetType.Assembly.GetType($"{targetType.Namespace}.{validatorTypeName}")
				?? throw new InvalidOperationException(
					$"No generated validator '{validatorTypeName}' found for type '{targetType.FullName}' in assembly '{assembly.GetName().Name}'."
				);
			if (Activator.CreateInstance(validatorType) is not IZodSchemaValidator validator)
				throw new InvalidOperationException(
					$"Generated validator '{validatorType.FullName}' does not implement IZodSchemaValidator."
				);

			factory.Register(targetType, validator);
		}

		return factory;
	}

	/// <summary>
	/// Scans <typeparamref name="T"/>'s assembly for <see cref="ZodSchemaGeneratedAttribute"/> and registers
	/// a generated <c>{TypeName}SchemaValidator</c> instance for each target type.
	/// Generated validators are expected to live in the same namespace as the target type and be named
	/// <c>{TypeName}SchemaValidator</c>.
	/// </summary>
	public static IZodSchemaFactory RegisterFromAssembly<T>(this IZodSchemaFactory factory) =>
		RegisterFromAssembly(factory, typeof(T).Assembly);
}
