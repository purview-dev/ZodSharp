# Newtonsoft.Json Integration

The `Purview.ZodSharp.NewtonsoftJson` package adds Newtonsoft.Json deserialize-and-validate, validating converters, and JSON Schema import to the core library. The deserialize/serialize extension methods live in the `ZodSharp` namespace; the JSON Schema import types (and `Z.FromJsonSchema`) live in the `ZodSharp.JsonSchema.NewtonsoftJson` namespace.

## Install

```bash
dotnet add package Purview.ZodSharp.NewtonsoftJson
```

## Deserialize and validate

```csharp
using ZodSharp;

var userSchema = Z.Object()
    .Field("name", Z.String().Min(3))
    .Field("age", Z.Number().Min(0).Int())
    .Build();

var json = """{ "name": "John", "age": 30 }""";

var result = userSchema.DeserializeAndValidate(json);
if (result.IsSuccess)
    Console.WriteLine($"Valid: {result.Value}");
```

Async stream and `JToken` overloads:

```csharp
await using var stream = File.OpenRead("user.json");
var result = await userSchema.DeserializeAndValidateAsync(stream);

var jToken = JObject.Parse(json);
var result2 = userSchema.DeserializeAndValidate(jToken);
```

## Validate and serialize

```csharp
var result = userSchema.ValidateAndSerialize(user); // ValidationResult<string>
var result2 = await userSchema.ValidateAndSerializeAsync(user, stream, formatting: Formatting.Indented);
```

## Validating converter

```csharp
var converter = userSchema.CreateValidatingConverter();
var value = JsonConvert.DeserializeObject<User>(json, converter);
```

`CreateValidatingConverter<T>()` returns a non-generic `Newtonsoft.Json.JsonConverter`. Invalid JSON throws `JsonSerializationException` with a `"Validation failed: ..."` message. The converter clones the `JsonSerializer` (without itself) to avoid recursion.

## API surface

| Member | Signature |
|---|---|
| `DeserializeAndValidate<T>` | `ValidationResult<T> DeserializeAndValidate<T>(this IZodSchema<T, T> schema, string json, JsonSerializerSettings? settings = null)` |
| `DeserializeAndValidate<T>` | `ValidationResult<T> DeserializeAndValidate<T>(this IZodSchema<T, T> schema, JToken token, JsonSerializer? serializer = null)` |
| `DeserializeAndValidateAsync<T>` | `Task<ValidationResult<T>> DeserializeAndValidateAsync<T>(this IZodSchema<T, T> schema, Stream jsonStream, JsonSerializerSettings? settings = null, CancellationToken cancellationToken = default)` |
| `ValidateAndSerialize<T>` | `ValidationResult<string> ValidateAndSerialize<T>(this IZodSchema<T, T> schema, T value, JsonSerializerSettings? settings = null, Formatting formatting = Formatting.None)` |
| `ValidateAndSerializeAsync<T>` | `Task<ValidationResult<string>> ValidateAndSerializeAsync<T>(this IZodSchema<T, T> schema, T value, Stream output, JsonSerializerSettings? settings = null, Formatting formatting = Formatting.Indented, CancellationToken cancellationToken = default)` |
| `CreateValidatingConverter<T>` | `JsonConverter CreateValidatingConverter<T>(this IZodSchema<T, T> schema)` |

## Failure codes

Deserialize/validation failures produce `ValidationError` entries with codes `deserialization_failed` and `json_error` in addition to the schema's own codes.

## JSON Schema import

```csharp
using ZodSharp;
using ZodSharp.JsonSchema.NewtonsoftJson;

var schema = Z.FromJsonSchema(jsonSchemaString);
```

See [JSON Schema Import](JsonSchema-Import.md) for the supported keywords, `$ref` handling, and the `JsonSchemaSerializerOptions` defaults.

## System.Text.Json vs Newtonsoft.Json

| Aspect | SystemTextJson | NewtonsoftJson |
|---|---|---|
| Async result type | `ValueTask<...>` | `Task<...>` |
| Options parameter | `System.Text.Json.JsonSerializerOptions` | `Newtonsoft.Json.JsonSerializerSettings` |
| Converter return | generic `JsonConverter<T>` | non-generic `JsonConverter` |
| `JToken` overload | no | yes |
| Formatting control | via `JsonSerializerOptions` | explicit `Newtonsoft.Json.Formatting` argument |
| Invalid-data exception | `System.Text.Json.JsonException` | `JsonSerializationException` |
| JSON plumbing | `JsonElement` | `JToken`/`JObject`/`JArray` |

> [!NOTE]
> `Purview.ZodSharp.SystemTextJson` and `Purview.ZodSharp.NewtonsoftJson` are mutually exclusive integrations — pick the one that matches your JSON library. Both packages can be referenced from the same project without `extern alias` (their JSON Schema types live in the `ZodSharp.JsonSchema.SystemTextJson` / `ZodSharp.JsonSchema.NewtonsoftJson` namespaces), but the deserialize/serialize extension overloads share names, so import exactly one package namespace per file.