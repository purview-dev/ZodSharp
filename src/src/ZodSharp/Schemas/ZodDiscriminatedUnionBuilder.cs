using System.Collections.Immutable;
using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Builder for creating discriminated unions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ZodDiscriminatedUnionBuilder class.
/// </remarks>
/// <param name="discriminator">The discriminator field name</param>
public class ZodDiscriminatedUnionBuilder(string discriminator)
{
	readonly Dictionary<string, IZodSchema<object, object>> _options = [];

	/// <summary>
	/// Adds an option to the discriminated union.
	/// </summary>
	/// <param name="value">The discriminator value</param>
	/// <param name="schema">The schema for this option</param>
	/// <returns>This builder for method chaining</returns>
	public ZodDiscriminatedUnionBuilder Option(string value, IZodSchema<object, object> schema)
	{
		_options[value] = schema;
		return this;
	}

	/// <summary>
	/// Builds the discriminated union schema.
	/// </summary>
	public ZodDiscriminatedUnion Build() => new(discriminator, _options.ToImmutableDictionary());
}
