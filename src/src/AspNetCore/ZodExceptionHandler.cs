using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ZodSharp.Core;

namespace ZodSharp.AspNetCore;

/// <summary>
/// An <see cref="IExceptionHandler"/> that converts a thrown <see cref="ZodException"/>
/// into a standard <see cref="HttpValidationProblemDetails"/> response,
/// resolving <see cref="ErrorType"/>s from the configured <see cref="ZodProblemDetailsOptions.Registry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Register with <c>services.AddZodSharpProblemDetails()</c> and ensure
/// <c>app.UseExceptionHandler()</c> is present in the pipeline, otherwise the handler is never invoked.
/// </para>
/// </remarks>
public sealed class ZodExceptionHandler : IExceptionHandler
{
	static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	readonly ZodProblemDetailsOptions _options;

	/// <summary>
	/// Initializes a new instance of the <see cref="ZodExceptionHandler"/> class.
	/// </summary>
	public ZodExceptionHandler(IOptions<ZodProblemDetailsOptions> options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_options = options.Value;
	}

	/// <inheritdoc/>
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(httpContext);

		if (exception is not ZodException zodException)
			return false;

		var defaultStatusCode =
			_options.StatusCodeSelector?.Invoke(zodException.Errors) ?? StatusCodes.Status400BadRequest;

		var problem = zodException.ToHttpValidationProblemDetails(_options.Registry, defaultStatusCode);
		problem.Extensions["traceId"] = httpContext.TraceIdentifier;

		httpContext.Response.StatusCode = problem.Status!.Value;
		await WriteProblemAsync(httpContext, problem, cancellationToken);

		return true;
	}

	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
		"Trimming",
		"IL2026:RequiresUnreferencedCode",
		Justification = "The serialized type is the concrete HttpValidationProblemDetails, not object, and is "
			+ "the same type ASP.NET Core's own problem-details writer emits, so a trimmed or Native AOT host "
			+ "already roots it. WriteAsJsonAsync carries the requirement unconditionally regardless of T."
	)]
	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
		"AOT",
		"IL3050:RequiresDynamicCode",
		Justification = "The serialized type is the concrete HttpValidationProblemDetails, not object, and is "
			+ "the same type ASP.NET Core's own problem-details writer emits, so a trimmed or Native AOT host "
			+ "already roots it. WriteAsJsonAsync carries the requirement unconditionally regardless of T."
	)]
	static Task WriteProblemAsync(
		HttpContext httpContext,
		HttpValidationProblemDetails problem,
		CancellationToken cancellationToken
	) => httpContext.Response.WriteAsJsonAsync(problem, JsonOptions, "application/problem+json", cancellationToken);
}
