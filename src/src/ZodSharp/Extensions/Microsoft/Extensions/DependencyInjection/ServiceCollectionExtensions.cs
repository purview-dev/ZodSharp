using System.ComponentModel;
using ZodSharp.Core;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering ZodSharp services with <see cref="IServiceCollection"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Adds the <see cref="IZodSchemaFactory"/> to the dependency injection container.
		/// </summary>
		/// <param name="configure">An optional callback to configure the factory.</param>
		/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
		public IServiceCollection AddZodSharpFactory(Action<IZodSchemaFactory>? configure = null)
		{
			services.AddSingleton<IZodSchemaFactory>(sp =>
			{
				ZodSchemaFactory factory = new();

				configure?.Invoke(factory);

				return factory;
			});

			return services;
		}
	}
}
