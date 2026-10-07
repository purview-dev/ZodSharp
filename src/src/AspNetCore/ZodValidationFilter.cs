using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ZodSharp.Core;

namespace ZodSharp.AspNetCore;

/// <summary>
/// An endpoint filter that validates the first bound argument of type <typeparamref name="T"/> against the
/// registered <see cref="IZodSchemaFactory"/>. When validation fails the request is short-circuited with a
/// standard validation-problem response.
/// </summary>
/// <typeparam name="T">The request type to validate.</typeparam>
public sealed class ZodValidationFilter<T> : IEndpointFilter
{
	/// <inheritdoc/>
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(next);

		var factory =
			context.HttpContext.RequestServices.GetService<IZodSchemaFactory>()
			?? throw new InvalidOperationException(
				"No IZodSchemaFactory is registered. Call AddZodSharp (or AddZodSharpFactory) before applying WithZodSharpValidation."
			);

		foreach (var argument in context.Arguments)
		{
			if (argument is not T value)
				continue;

			var validator =
				factory.Resolve<T>()
				?? throw new InvalidOperationException(
					$"No Zod schema validator is registered for type '{typeof(T).FullName}'."
				);

			var result = await validator.ValidateAsync(value, context.HttpContext.RequestAborted);
			if (!result.IsSuccess)
				return result.ToValidationProblem();

			break;
		}

		return await next(context);
	}
}
