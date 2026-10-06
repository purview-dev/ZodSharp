namespace ZodSharp.JsonSchema;

/// <summary>
/// A non-generic view of an array schema, for the JSON Schema exporter.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ToJsonSchemaConverter"/> walks schemas through a non-generic <see cref="object"/>, so it cannot
/// pattern-match <c>ZodArray&lt;T&gt;</c> without knowing <c>T</c>. It used to bridge that gap by matching on
/// the type's <em>name</em> and then reading private fields by <em>name</em> — and both names were wrong:
/// it looked for <c>_elementSchema</c> (the element schema is a primary-constructor parameter, so no such
/// field exists) and for <c>_minItems</c>/<c>_maxItems</c> on rules called <c>MinItemsRule</c>/
/// <c>MaxItemsRule</c> that have never existed. Every lookup returned null, so array element schemas and
/// bounds were silently missing from every exported schema.
/// </para>
/// <para>
/// This interface is the replacement: a type-safe, non-generic seam that the compiler checks, costs no
/// reflection, and is safe under trimming and Native AOT. It is deliberately <c>internal</c> — it is a
/// cross-type detail of this assembly, not public API.
/// </para>
/// </remarks>
interface IJsonSchemaArrayInfo
{
	/// <summary>
	/// Gets the minimum element count, or null when unbounded.
	/// </summary>
	int? MinItems { get; }

	/// <summary>
	/// Gets the maximum element count, or null when unbounded.
	/// </summary>
	int? MaxItems { get; }

	/// <summary>
	/// Gets the element schema, boxed so the member is reachable without naming the element type.
	/// </summary>
	object ElementSchema { get; }
}
