namespace ZodSharp.JsonSchema;

/// <summary>
/// A non-generic view of a schema that wraps another schema, for the JSON Schema exporter.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ToJsonSchemaConverter"/> dispatches on a non-generic <see cref="object"/>, so a generic wrapper
/// such as <c>FieldSchemaWrapper&lt;T&gt;</c> cannot be pattern-matched without naming <c>T</c>. Before this
/// seam existed, a wrapper fell through to the generic fallback and produced a definition with no type at
/// all — which is why every property of an exported object schema came back empty: the object builder wraps
/// each field schema to present it untyped.
/// </para>
/// <para>
/// Implement this on anything that merely adapts or decorates another schema without changing the exported
/// JSON Schema shape, so the exporter can unwrap to the schema that does determine it.
/// </para>
/// </remarks>
interface IJsonSchemaInnerSchema
{
	/// <summary>
	/// Gets the wrapped schema, boxed so the member is reachable without naming its type.
	/// </summary>
	object InnerSchema { get; }
}
