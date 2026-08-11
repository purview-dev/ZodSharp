using ZodSharp.Core;
using ZodSharp.JsonSchema;

namespace ZodSharp;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible"
)]
#if !NETSTANDARD2_1_OR_GREATER
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class ZExtensions
{
	extension(Z)
	{
		/// <summary>
		/// Creates a ZodSharp schema from a JSON Schema definition.
		/// Enables consuming schemas defined in TypeScript Zod.
		/// </summary>
		public static IZodSchema<object, object> FromJsonSchema(
			JsonSchemaDefinition schema,
			FromJsonSchemaOptions? options = null
		) => FromJsonSchemaParser.Parse(schema, options);

		/// <summary>
		/// Creates a ZodSharp schema from a JSON Schema string.
		/// Enables consuming schemas defined in TypeScript Zod via JSON files or APIs.
		/// </summary>
		public static IZodSchema<object, object> FromJsonSchema(
			string jsonSchema,
			FromJsonSchemaOptions? options = null
		) => FromJsonSchemaParser.Parse(jsonSchema, options);
	}
}
