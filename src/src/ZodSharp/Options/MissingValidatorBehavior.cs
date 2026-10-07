using Microsoft.Extensions.Options;
using ZodSharp.Core;

namespace ZodSharp.Options;

/// <summary>
/// Defines how <see cref="ZodSchemaOptionsValidator{T}"/> behaves when no schema validator is
/// registered for the options type.
/// </summary>
public enum MissingValidatorBehavior
{
	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> via <see cref="IZodSchemaFactory.ResolveRequired{T}"/>
	/// when no validator is registered for the options type.
	/// </summary>
	Throw,

	/// <summary>
	/// Treats an unregistered options type as valid, returning <see cref="ValidateOptionsResult.Success"/>.
	/// </summary>
	Ignore,
}
