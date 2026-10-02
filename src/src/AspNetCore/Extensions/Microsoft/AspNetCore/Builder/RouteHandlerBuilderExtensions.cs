using Microsoft.AspNetCore.Http;
using ZodSharp.AspNetCore;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Minimal API integration for ZodSharp validation.
/// </summary>
public static class ZodSharpRouteHandlerBuilderExtensions
{
	/// <summary>
	/// Adds an endpoint filter that validates the first bound argument of type <typeparamref name="T"/> against
	/// the registered <see cref="ZodSharp.Core.IZodSchemaFactory"/>, returning a validation-problem response when
	/// validation fails.
	/// </summary>
	/// <typeparam name="T">The request type to validate.</typeparam>
	/// <param name="builder">The route handler builder.</param>
	/// <returns>The builder for chaining.</returns>
	public static RouteHandlerBuilder WithZodSharpValidation<T>(this RouteHandlerBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.AddEndpointFilter<ZodValidationFilter<T>>();
		return builder;
	}
}
