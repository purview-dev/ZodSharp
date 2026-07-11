# ZodSharp

[![NuGet version](https://img.shields.io/nuget/v/ZodSharp.svg)](https://www.nuget.org/packages/ZodSharp)

**ZodSharp** is a high-performance schema validation library for C#, ported from TypeScript [Zod](https://github.com/colinhacks/zod). It features zero-allocation validation, struct-based rules, fluent API, and source generator support for maximum performance.

## Key Features

- **Zero-allocation validation** - Minimizes allocations using structs and `Span<T>`
- **Struct-based rules** - Validation rules implemented as structs to avoid GC
- **Fluent API** - Fluent and extensible API similar to original Zod
- **Type-safe** - Strong typing with advanced C# generics
- **High performance** - Sub-microsecond validation times, 10x faster than reflection-based validation
- **Cross-platform** - Works on .NET 9.0 and .NET Standard 2.1
- **Source Generators** - Compile-time validator generation with `[ZodSchema]` attribute
- **DataAnnotations Support** - Automatic validation from `[Required]`, `[StringLength]`, `[Length]`, `[MinLength]`, `[MaxLength]`, `[Range]`, `[RegularExpression]`, `[AllowedValues]`, `[DeniedValues]`, `[EmailAddress]`, etc.  

## Installation

### NuGet Package Manager
```powershell
Install-Package ZodSharp
```

### .NET CLI
```bash
dotnet add package ZodSharp
```

### PackageReference
```xml
<PackageReference Include="ZodSharp" Version="1.0.0" />
```

## Usage Examples

### Basic Validation

```csharp
using ZodSharp;
using ZodSharp.Core;

// String validation
var nameSchema = Z.String().Min(3).Max(50);
var result = nameSchema.Validate("John");
if (result.IsSuccess)
{
    Console.WriteLine($"Valid name: {result.Value}");
}

// Number validation
var ageSchema = Z.Number().Min(0).Max(120).Int();
var ageResult = ageSchema.Validate(25.0);

// Additional number validations
var positiveSchema = Z.Number().Positive();
var negativeSchema = Z.Number().Negative();
var multipleOfSchema = Z.Number().MultipleOf(10); // Must be multiple of 10
var finiteSchema = Z.Number().Finite(); // Not Infinity
var safeSchema = Z.Number().Safe(); // Safe integer

// Email validation
var emailSchema = Z.String().Email();
var emailResult = emailSchema.Validate("user@example.com");

// URL validation
var urlSchema = Z.String().Url();
var urlResult = urlSchema.Validate("https://example.com");

// UUID validation
var uuidSchema = Z.String().Uuid();
var uuidResult = uuidSchema.Validate("550e8400-e29b-41d4-a716-446655440000");

// String transformations
var trimmedSchema = Z.String().Trim();
var upperSchema = Z.String().ToUpper();
var lowerSchema = Z.String().ToLower();

// String prefixes and suffixes
var prefixSchema = Z.String().StartsWith("https://");
var suffixSchema = Z.String().EndsWith(".com");

// Exact length
var exactLengthSchema = Z.String().Length(10);
```

### Object Validation

```csharp
var userSchema = Z.Object()
    .Field("name", Z.String().Min(1))
    .Field("age", Z.Number().Min(0).Max(120))
    .Field("email", Z.String().Email())
    .Build();

var userData = new Dictionary<string, object?>
{
    { "name", "John Doe" },
    { "age", 30.0 },
    { "email", "john@example.com" }
};

var result = userSchema.Validate(userData);
if (result.IsSuccess)
{
    var validatedUser = result.Value;
    // Use validatedUser...
}
```

### Array Validation

```csharp
var numbersSchema = Z.Array(Z.Number()).Min(1).Max(10);
var result = numbersSchema.Validate(new[] { 1.0, 2.0, 3.0 });

// Exact length
var exactLengthSchema = Z.Array(Z.String()).Length(5);

// Non-empty array
var nonEmptySchema = Z.Array(Z.String()).NonEmpty();
```

### Optional Fields

```csharp
var optionalSchema = Z.Optional(Z.String());
var result1 = optionalSchema.Validate(null); // Success
var result2 = optionalSchema.Validate("value"); // Success
```

### Error Handling

```csharp
try
{
    var value = nameSchema.Parse("AB"); // Too short - throws
}
catch (ZodException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"  - {string.Join(".", error.Path)}: {error.Message}");
    }
}

// Or use SafeParse for non-throwing validation
var result = nameSchema.SafeParse("AB");
if (!result.IsSuccess)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
```

## Performance

ZodSharp is designed for maximum performance with zero-allocation validation and struct-based rules. Here's what makes it fast:

### Performance Characteristics

**Typical validation times** (measured on .NET 9.0, Release mode):
- Simple string validation: **~50-100 ns** per validation
- Number validation: **~30-80 ns** per validation
- Small arrays (10 items): **~500-800 ns** per validation
- Medium objects (6 fields): **~1-2 μs** per validation
- Complex objects (13 fields with nesting): **~3-5 μs** per validation

**Memory efficiency**:
- Zero allocations for simple validations (strings, numbers, booleans)
- Minimal allocations for arrays and objects (only for error collections)
- Struct-based rules avoid GC pressure
- No reflection overhead in hot paths

### Performance Optimizations

ZodSharp implements several optimizations for maximum performance:

#### 1. Zero-allocation Validation
- Validation rules implemented as `struct` to avoid allocations
- Use of `Span<T>` and `ReadOnlySpan<T>` when appropriate
- Object pooling for reusable schemas

#### 2. Struct-based Rules
All validation rules are structs:

```csharp
public readonly struct MinLengthRule : IValidationRule<string>
{
    // Zero allocation validation
}
```

#### 3. Compiled Validators
Use expression trees to compile validators at runtime for maximum speed:

```csharp
using ZodSharp.Expressions;

var compiled = CompiledValidator.Compile(schema);
var result = compiled(value); // Ultra-fast validation
```

#### 4. Fluent API
Fluent API that allows schema composition:

```csharp
var schema = Z.String()
    .Min(3)
    .Max(50)
    .Email()
    .Describe("User email address");
```

### Performance Benchmarks

We maintain comprehensive performance tests in the `performance/` folder. Run them yourself:

```bash
# Run all performance benchmarks
dotnet run --project performance/performance.csproj -c Release

# Run specific test suites
dotnet run --project performance/performance.csproj -c Release --filter "*MemoryPerformanceTests*"
```

**Key performance highlights**:
- **10x faster** than reflection-based validation libraries
- **Zero allocations** for primitive validations
- **Sub-microsecond** validation for simple types
- **Minimal GC pressure** with struct-based architecture
- **Scalable** performance even with complex nested schemas

See the [performance README](performance/README.md) for detailed benchmark results and optimization tips.

## Architecture

```
ZodSharp/
├── Core/              # Base interfaces and classes
│   ├── IZodSchema.cs
│   ├── ZodType.cs
│   ├── ValidationResult.cs
│   └── ValidationError.cs
├── Schemas/           # Schema implementations
│   ├── ZodString.cs
│   ├── ZodNumber.cs
│   ├── ZodObject.cs
│   └── ...
├── Rules/             # Validation rules (structs)
│   ├── MinLengthRule.cs
│   ├── EmailRule.cs
│   └── ...
└── Optimizations/     # Optimization helpers
    └── ZeroAllocationHelpers.cs
```

## Advanced Features

### Transforms
Transform values during validation:

```csharp
var schema = Z.String().Transform(s => s.ToUpper());
var result = schema.Validate("hello"); // "HELLO"
```

### Refinements
Add custom validations:

```csharp
var schema = Z.Number().Refine(n => n % 2 == 0, "Must be even");
var result = schema.Validate(4); // Success
```

### Lazy Evaluation
Create recursive and circular schemas:

```csharp
var categorySchema = Z.Lazy<Dictionary<string, object?>>(() => 
    Z.Object()
        .Field("name", Z.String())
        .Field("subcategories", Z.Array(categorySchema))
        .Build()
);
```

### Discriminated Unions
Optimized unions with discriminator:

```csharp
var union = Z.DiscriminatedUnion("type")
    .Option("user", userSchema)
    .Option("admin", adminSchema)
    .Build();
```

### Default Values
Default values when input is null:

```csharp
var schema = Z.String().Default("unknown");
var result = schema.Validate(null); // "unknown"
```

### JSON Integration
Integrated JSON validation with Newtonsoft.Json:

```csharp
using ZodSharp.Json;

// Deserialize and validate from string
var result = schema.DeserializeAndValidate(jsonString);

// Deserialize and validate from stream (async)
var result2 = await schema.DeserializeAndValidateAsync(jsonStream);

// Deserialize and validate from JToken
var result3 = schema.DeserializeAndValidate(jToken);

// Create JsonConverter with validation
var converter = schema.CreateValidatingConverter();
```

### JSON Schema Interoperability

Share schemas between TypeScript (Zod) and C# (ZodSharp) using JSON Schema. This enables infinite interoperability, allowing you to define a schema in one language and reuse it in another.

#### Export to JSON Schema (ZodSharp -> JSON Schema)

```csharp
var userSchema = Z.Object()
    .Field("name", Z.String().Min(3))
    .Field("email", Z.String().Email())
    .Field("age", Z.Number().Min(0).Int())
    .Build();

// Convert to JSON Schema object
var jsonSchema = Z.ToJsonSchema<Dictionary<string, object?>>(userSchema, new ToJsonSchemaOptions 
{ 
    Title = "User",
    Id = "https://example.com/schemas/user.json" 
});

// Serialize to string
var jsonString = JsonConvert.SerializeObject(jsonSchema, JsonSchemaSerializerOptions.Default);
```

#### Import from JSON Schema (JSON Schema -> ZodSharp)

```csharp
var jsonSchemaString = @"{
    ""type"": ""object"",
    ""properties"": {
        ""name"": { ""type"": ""string"", ""minLength"": 3 },
        ""email"": { ""type"": ""string"", ""format"": ""email"" }
    },
    ""required"": [""name"", ""email""]
}";

// Parse into ZodSharp schema
var userSchema = Z.FromJsonSchema(jsonSchemaString);

// Validate data
var result = userSchema.Validate(userData);
```

#### Cross-Platform Scenario

**Frontend (TypeScript/Zod):**
```typescript
import { z } from "zod";

const UserSchema = z.object({
  username: z.string().min(3),
  email: z.string().email()
});

// Zod v4+ natively supports JSON Schema conversion
const jsonSchema = z.toJSONSchema(UserSchema);
// Send jsonSchema to backend...
```

**Backend (C#/ZodSharp):**
```csharp
// Receive jsonSchema...
var userSchema = Z.FromJsonSchema(jsonSchemaString);
var result = userSchema.Validate(incomingData);
```

### Compiled Validators
Compiled validators for maximum performance:

```csharp
using ZodSharp.Expressions;

var compiled = CompiledValidator.Compile(schema);
var result = compiled(value); // Ultra-fast validation
```

### Schema Caching
Intelligent schema caching:

```csharp
using ZodSharp.Core;

var schema = SchemaCache.GetOrCreate("user", () => 
    Z.Object().Field("name", Z.String()).Build()
);
```

### Source Generators
Generate zero-allocation validators at compile time:

```csharp
using System.ComponentModel.DataAnnotations;
using ZodSharp.SourceGenerators;

[ZodSchema]
public class User
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0, 120)]
    public int Age { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}

// Auto-generated validator
var result = UserSchema.Validate(user);
var validated = UserSchema.Parse(user); // Throws on failure

// Composition methods
var refined = UserSchema.Refine(user, u => u.Age >= 18, "Must be adult");
var combined = UserSchema.And(user, u => u.Name.Length > 5, "Name too short");
```

**Features:**
- Automatic validation from DataAnnotations attributes
- Zero-reflection, zero-allocation validators
- Composition methods (`.And()`, `.Or()`, `.Refine()`)
- Supports classes, structs, and records

#### Supported DataAnnotations size validators

`[Length]`, `[StringLength]`, `[MinLength]`, and `[MaxLength]` generate direct `Length` or `Count` access when possible:

- `string` -> `.Length`
- arrays, including rectangular arrays -> `.Length`
- jagged arrays -> outer-array `.Length`
- countable collections -> `.Count`
- `IEnumerable` / `IEnumerable<T>` -> a single counted pass with a non-enumerating fast path

`[Length]` follows DataAnnotations null semantics: `null` is valid unless `[Required]` is also present.

Other supported DataAnnotations validators:

- `[Range]` on numeric types plus parsed `decimal`, `DateTime`, `DateOnly`, and `TimeOnly` bounds
- `[RegularExpression]` on strings, with DataAnnotations-compatible `null` and empty-string behaviour
- `[AllowedValues]` and `[DeniedValues]` using generated typed equality checks instead of runtime attribute execution
- `[EmailAddress]` on strings

Structured size failures expose:

- `Code`: `too_small` or `too_big`
- `Origin`: `string`, `array`, or `collection`
- `Minimum` / `Maximum`
- `Inclusive`
- `Path`

Example:

```csharp
using System.ComponentModel.DataAnnotations;

[ZodSchema]
public sealed class Basket
{
    [Required]
    [Length(2, 5)]
    public List<string>? Items { get; set; }
}

var result = BasketSchema.Validate(new Basket { Items = ["apple"] });
// result.Errors[0].Code == "too_small"
// result.Errors[0].Minimum == 2
```

### ASP.NET Core ProblemDetails

Install `ZodSharp.AspNetCore` to convert failed validation results into standard ASP.NET Core payloads while preserving structured issues:

```csharp
using ZodSharp.AspNetCore;

var result = BasketSchema.Validate(basket);

if (!result.IsSuccess)
{
    var problem = result.ToHttpValidationProblemDetails();
    return Results.ValidationProblem(
        problem.Errors,
        extensions: new Dictionary<string, object?>
        {
            ["issues"] = problem.Extensions["issues"]
        });
}
```

### Span<T> Validation
Zero-allocation string validation using spans:

```csharp
var schema = Z.String().Min(3).Max(50).Email();
var span = "user@example.com".AsSpan();
var result = schema.ValidateSpan(span);
```

## License

MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please open an issue or pull request.

## Acknowledgments

- [Zod](https://github.com/colinhacks/zod)
