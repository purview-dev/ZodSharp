using Microsoft.Extensions.DependencyInjection;
using ZodSharp.Core;

namespace ZodSharp.AspNetCore;

/// <summary>
/// DI integration for ZodSharp schema validation.
/// </summary>
public static class ZodSharpServiceCollectionExtensions
{
	/// <summary>
	/// Registers <see cref="IZodSchemaFactory"/> as a singleton, applies configuration,
	/// and auto-registers source-generated validators from the configured assemblies.
	/// </summary>
	public static IServiceCollection AddZodSharp(
		this IServiceCollection services,
		Action<ZodSchemaFactoryOptions>? configure = null
	)
	{
		var options = new ZodSchemaFactoryOptions();
		configure?.Invoke(options);

		services.AddSingleton<IZodSchemaFactory>(sp =>
		{
			var factory = new ZodSchemaFactory();
			options.ConfigureFactory?.Invoke(factory);
			foreach (var assembly in options.ScanAssemblies)
				factory.RegisterFromAssembly(assembly);
			return factory;
		});

		return services;
	}
}
