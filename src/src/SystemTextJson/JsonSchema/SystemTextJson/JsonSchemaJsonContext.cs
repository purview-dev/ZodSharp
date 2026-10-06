using System.Text.Json.Serialization;

namespace ZodSharp.JsonSchema.SystemTextJson;

/// <summary>
/// Source-generated serialization contracts for <see cref="JsonSchemaDefinition"/>.
/// </summary>
/// <remarks>
/// <para>
/// JSON Schema import and export serialize a type this library owns, so the contract can be generated at
/// compile time instead of resolved reflectively. That makes both directions safe under trimming and Native
/// AOT with nothing required from the consumer — they do not have to register our type in their own
/// <see cref="JsonSerializerContext"/>.
/// </para>
/// <para>
/// The camelCase policy here matches what <see cref="JsonSchemaSerializerOptions"/> previously applied at
/// runtime. The four <c>$</c>-prefixed keyword names are not expressible as a policy, so they are declared
/// with <see cref="JsonPropertyNameAttribute"/> on <see cref="JsonSchemaDefinition"/> itself and are
/// honoured identically by the generated contract and by the reflection resolver.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(JsonSchemaDefinition))]
partial class JsonSchemaJsonContext : JsonSerializerContext { }
