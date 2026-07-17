using System.ComponentModel;
using ZodSharp.Core;

namespace Microsoft.Extensions.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
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
