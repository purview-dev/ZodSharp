# Zod-style Composition in ZodSharp Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Implement Zod-style composition patterns (e.g., `z.object().extend()`, `z.union()`, `z.intersection()`) within the ZodSharp C# library.

**Architecture:** This will involve extending existing schema types and introducing new ones to represent composition. We will leverage discriminated unions and generic patterns in C# where appropriate to model these concepts effectively. The approach will prioritize type safety and a developer experience mirroring Zod.

**Tech Stack:** C#, .NET

---

### Task 1: Define Schema Composition Base Interfaces/Abstract Classes

**Objective:** Establish the foundational types that will represent the composition of schemas.

**Files:**
- Create: `src/ZodSharp.NET.Schema/Types/Composition/ICompositionSchema.cs`
- Create: `src/ZodSharp.NET.Schema/Types/Composition/CompositionSchemaBase.cs`
- Modify: `src/ZodSharp.NET.Schema/IZodSchema.cs` (to potentially mark schemas that can be composed)

**Step 1: Define `ICompositionSchema` interface**
This interface will mark schemas that can participate in composition operations.

```csharp
// src/ZodSharp.NET.Schema/Types/Composition/ICompositionSchema.cs
namespace ZodSharp.NET.Schema.Types.Composition
{
    /// <summary>
    /// Represents a schema that can be composed with other schemas.
    /// </summary>
    public interface ICompositionSchema
    {
        // Methods for composition will be defined here or on derived types.
        // This might include methods like Extend, Union, Intersection.
    }
}
```

**Step 2: Define `CompositionSchemaBase` abstract class**
This abstract class can provide common functionality for composed schemas.

```csharp
// src/ZodSharp.NET.Schema/Types/Composition/CompositionSchemaBase.cs
using ZodSharp.NET.Schema.Types;

namespace ZodSharp.NET.Schema.Types.Composition
{
    /// <summary>
    /// Base abstract class for schemas supporting composition.
    /// </summary>
    public abstract class CompositionSchemaBase<T> : IZodSchema<T>, ICompositionSchema
    {
        public abstract T Parse(object input);
        public abstract T SafeParse(object input, out ErrorResult? errorResult);
        // Potentially other common IZodSchema methods or composition helpers defined here.
    }
}
```

**Step 3: Update `IZodSchema` (Optional but Recommended)**
Consider adding a marker interface or a property to `IZodSchema` to indicate compisition capabilities if not all schemas will support it. For now, we'll rely on `ICompositionSchema`.

**Step 4: Write tests for base types**
Create tests to ensure the interface and abstract class are correctly defined.

```bash
# Run specific tests after implementation
# (This command assumes a testing framework like xUnit or NUnit is configured)
# Placeholder command, actual command depends on project setup.
# Example: dotnet test --filter "FullyQualifiedName~ZodSharp.NET.Schema.Tests.Unit.Types.Composition.CompositionSchemaBaseTests"
```

**Step 5: Commit**
```bash
git add src/ZodSharp.NET.Schema/Types/Composition/ICompositionSchema.cs
git add src/ZodSharp.NET.Schema/Types/Composition/CompositionSchemaBase.cs
# git add src/ZodSharp.NET.Schema/IZodSchema.cs (if modified)
git commit -m "feat: introduce ICompositionSchema and CompositionSchemaBase"
```

### Task 2: Implement `.extend()` for ZodObject

**Objective:** Allow extending an existing object schema with new properties.

**Files:**
- Create: `src/ZodSharp.NET.Schema/Types/Composition/ObjectExtensionSchema.cs`
- Modify: `src/ZodSharp.NET.Schema/Types/ObjectSchema.cs` (to add the `Extend` method)

**Step 1: Define `ObjectExtensionSchema<TOriginal, TExtension>`**
This schema will combine an original object schema with an extension object schema.

```csharp
// src/ZodSharp.NET.Schema/Types/Composition/ObjectExtensionSchema.cs
using ZodSharp.NET.Schema.Types;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ZodSharp.NET.Schema.Types.Composition
{
    /// <summary>
    /// Represents an object schema that extends an existing object schema.
    /// TOriginal: The type of the original object schema.
    /// TExtension: The type of the extension schema.
    /// TCombined: The resulting combined type.
    /// </summary>
    public class ObjectExtensionSchema<TOriginal, TExtension, TCombined> : CompositionSchemaBase<TCombined>
        where TOriginal : IZodSchema<TOriginal>
        where TExtension : IZodSchema<TExtension>
        // TCombined is typically an anonymous type or a new DTO,
        // but for simplicity in initial implementation, we might use dynamic or object.
        // A more robust solution would involve generating types or using System.Text.Json.JsonElement.
    {
        private readonly TOriginal _originalSchema;
        private readonly TExtension _extensionSchema;

        public ObjectExtensionSchema(TOriginal originalSchema, TExtension extensionSchema)
        {
            _originalSchema = originalSchema;
            _extensionSchema = extensionSchema;
        }

        public override TCombined Parse(object input)
        {
            // Implementation will involve parsing input against both schemas and merging.
            // For current implementation, consider using JsonElement for dynamic merging.
            throw new NotImplementedException();
        }

        public override TCombined SafeParse(object input, out ErrorResult? errorResult)
        {
            // Similar to Parse, but with error handling.
            throw new NotImplementedException();
        }
    }
}
```

**Step 2: Add `Extend` method to `ObjectSchema<T>`**
This method will create and return an `ObjectExtensionSchema`.

```csharp
// src/ZodSharp.NET.Schema/Types/ObjectSchema.cs (within ObjectSchema<T> class)
// ... other usings

using ZodSharp.NET.Schema.Types.Composition;
using System.Collections.Generic;
using System.Linq;

public class ObjectSchema<T> : SchemaBase<T>, IZodSchema<T> // Assuming SchemaBase is your base
{
    // ... existing properties and methods

    /// <summary>
    /// Extends the current object schema with new properties defined by the extensionSchema.
    /// </summary>
    /// <typeparam name="TExtension">The type of the extension schema.</typeparam>
    /// <typeparam name="TCombined">The resulting combined type.</typeparam>
    /// <param name="extensionSchema">The schema defining the properties to add.</param>
    /// <returns>A new schema representing the extended object.</returns>
    public ObjectSchema<TCombined> Extend<TExtension, TCombined>(ObjectSchema<TExtension> extensionSchema)
        where TExtension : new() // Constraint might need adjustment based on how TCombined is formed
        where TCombined : new()
    {
        // The logic here is complex: it needs to infer TCombined and merge the schemas.
        // For initial implementation, this might return a schema that delegates parsing to both.
        // A more advanced implementation might involve runtime type generation or using dynamic types.
        // For now, we'll return a generic composition schema.
        return new ObjectSchema<TCombined>(new ObjectExtensionSchema<T, TExtension, TCombined>(this, extensionSchema));
    }

    // Consider overloads for different scenarios, e.g., extending with an anonymous object definition.
}
```
*Note: The implementation of `ObjectExtensionSchema.Parse` and `ObjectSchema.Extend` is highly complex due to C#'s type system and the need to merge object structures dynamically or at compile time. This plan outlines the structure; full implementation will require careful consideration of generics, `System.Text.Json.JsonElement`, or potentially runtime code generation.*

**Step 3: Write tests for `Extend` functionality**
Test that extending an object schema works as expected, including merging properties and handling potential conflicts (though conflict resolution is out of scope for the initial implementation).

```bash
# Placeholder for testing the Extend functionality
# dotnet test --filter "FullyQualifiedName~ZodSharp.NET.Schema.Tests.Unit.Types.Composition.ObjectSchemaExtensionTests"
```

**Step 4: Commit**
```bash
git add src/ZodSharp.NET.Schema/Types/Composition/ObjectExtensionSchema.cs
git add src/ZodSharp.NET.Schema/Types/ObjectSchema.cs
git commit -m "feat: implement object schema extension with Extend method"
```

### Task 3: Implement `z.union()`

**Objective:** Create a schema that accepts values conforming to any of several sub-schemas.

**Files:**
- Create: `src/ZodSharp.NET.Schema/Types/Composition/UnionSchema.cs`
- Modify: `ZodSharp.NET.Schema/NamespaceImports.cs` (or similar file for Zod factory methods) to add `Zod.Union`

**Step 1: Define `UnionSchema<T>`**
This schema will accept a list of schemas and attempt to parse the input against each one in order.

```csharp
// src/ZodSharp.NET.Schema/Types/Composition/UnionSchema.cs
using ZodSharp.NET.Schema.Types;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ZodSharp.NET.Schema.Types.Composition
{
    /// <summary>
    /// Represents a schema that combines multiple schemas using a union.
    /// The input is parsed against each schema in order until one succeeds.
    /// </summary>
    public class UnionSchema<T> : CompositionSchemaBase<T> // T is the union type, potentially object or a base class
    {
        private readonly List<IZodSchema<T>> _schemas;

        public UnionSchema(IEnumerable<IZodSchema<T>> schemas)
        {
            _schemas = schemas.ToList();
            if (!_schemas.Any())
            {
                throw new ArgumentException("Union schema must have at least one schema.", nameof(schemas));
            }
        }

        public override T Parse(object input)
        {
            ErrorResult? firstError = null;
            foreach (var schema in _schemas)
            {
                try
                {
                    // This assumes T is compatible across all schemas, or we use object/dynamic
                    return schema.Parse(input);
                }
                catch (ZodSchemaException ex)
                {
                    // Collect the first error encountered, or a more sophisticated error aggregation
                    if (firstError == null)
                    {
                        firstError = ex.ErrorResult;
                    }
                }
            }

            // If no schema parsed the input, throw an exception with aggregated errors.
            throw new ZodSchemaException(
                $"Input did not match any of the union schemas. First error: {firstError?.Message ?? "Unknown error"}.",
                firstError // Potentially aggregate all errors here
            );
        }

        public override T SafeParse(object input, out ErrorResult? errorResult)
        {
            ErrorResult? firstError = null;
            foreach (var schema in _schemas)
            {
                var parsed = schema.SafeParse(input, out var currentError);
                if (currentError == null)
                {
                    errorResult = null;
                    return parsed;
                }
                if (firstError == null)
                {
                    firstError = currentError;
                }
            }

            errorResult = new ErrorResult($"Input did not match any of the union schemas. First error: {firstError?.Message ?? "Unknown error"}.", FirstError: firstError);
            return default!; // Return default for type T
        }
    }
}
```

**Step 2: Add `Zod.Union` factory method**
This method will be part of the main `Zod` class, acting as the entry point for creating union schemas.

```csharp
// src/ZodSharp.NET.Schema/NamespaceImports.cs (or Zod.cs)
// ... other using statements

public static class Zod
{
    // ... other factory methods

    /// <summary>
    /// Creates a union schema that accepts values conforming to any of the provided schemas.
    /// </summary>
    /// <typeparam name="T">The resulting type after parsing. This should be a common supertype or 'object'.</typeparam>
    /// <param name="schemas">The schemas to include in the union.</param>
    /// <returns>A new UnionSchema.</returns>
    public static UnionSchema<T> Union<T>(params IZodSchema<T>[] schemas)
    {
        return new UnionSchema<T>(schemas);
    }

    // Consider overloads for different numbers of schemas or specific types.
}
```
*Note: The generic parameter `T` for `UnionSchema` is tricky. If the schemas are of different types (e.g., `string` and `number`), `T` would likely need to be `object` or a custom union type struct/class. Handling type safety and `T` inference requires careful design.*

**Step 3: Write tests for `UnionSchema`**
Test that it correctly parses inputs matching each schema and fails when no schema matches.

```bash
# Placeholder for testing UnionSchema
# dotnet test --filter "FullyQualifiedName~ZodSharp.NET.Schema.Tests.Unit.Types.Composition.UnionSchemaTests"
```

**Step 4: Commit**
```bash
git add src/ZodSharp.NET.Schema/Types/Composition/UnionSchema.cs
git add src/ZodSharp.NET.Schema/NamespaceImports.cs # or wherever Zod properties are defined
git commit -m "feat: implement Zod.union schema composition"
```

### Task 4: Implement `z.intersection()`

**Objective:** Create a schema that requires values to conform to all of several sub-schemas.

**Files:**
- Create: `src/ZodSharp.NET.Schema/Types/Composition/IntersectionSchema.cs`
- Modify: `ZodSharp.NET.Schema/NamespaceImports.cs` (or similar file for Zod factory methods) to add `Zod.Intersection`

**Step 1: Define `IntersectionSchema<T>`**
This schema will attempt to parse the input against all provided schemas sequentially. The output type `T` will likely be `object` or a merged representation.

```csharp
// src/ZodSharp.NET.Schema/Types/Composition/IntersectionSchema.cs
using ZodSharp.NET.Schema.Types;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ZodSharp.NET.Schema.Types.Composition
{
    /// <summary>
    /// Represents a schema that combines multiple schemas using an intersection.
    /// The input must conform to ALL provided schemas.
    /// </summary>
    public class IntersectionSchema<T> : CompositionSchemaBase<T> // T implies a merged type, likely object or dynamic
    {
        private readonly List<IZodSchema<object>> _schemas; // Using object as input type for flexibility
        private readonly int _schemaCount;

        public IntersectionSchema(IEnumerable<IZodSchema<object>> schemas)
        {
            _schemas = schemas.ToList();
            if (!_schemas.Any())
            {
                throw new ArgumentException("Intersection schema must have at least one schema.", nameof(schemas));
            }
            _schemaCount = _schemas.Count;
        }

        public override T Parse(object input)
        {
            object currentInput = input;
            // In a real implementation, we'd need a way to merge results or ensure type compatibility.
            // For now, we'll just validate that each schema can parse the input.
            // The output type T makes this complex. We might return object and rely on downstream casting.
            foreach (var schema in _schemas)
             {
                // This simple loop only validates. Merging complex objects is much harder.
                // For object schemas, this would involve merging properties.
                schema.Parse(currentInput); // Throws if any schema fails
            }

            // If all schemas passed, return the input (or a merged representation if supported).
            // This is a simplification. A true intersection might require custom type logic.
            return (T)input; // This cast may fail if T is not compatible with input object type.
        }

        public override T SafeParse(object input, out ErrorResult? errorResult)
        {
            object currentInput = input;
            ErrorResult? firstError = null;

            for (int i = 0; i < _schemas.Count; i++)
            {
                var schema = _schemas[i];
                var parsed = schema.SafeParse(currentInput, out var currentError);
                if (currentError != null)
                {
                    if (firstError == null)
                    {
                        firstError = new ErrorResult($"Intersection failed at schema {i+1}/{_schemaCount}. Inner error: {currentError.Message}", SchemaPath: i.ToString(), FirstError: currentError);
                    }
                    errorResult = firstError;
                    return default!; // Return default for type T
                }
                // In a real implementation, `parsed` could be used to build up a merged object.
                // For simplicity here, we just ensure the input is valid against all.
                currentInput = parsed!; // For complex merging, this would be more involved.
            }

            errorResult = null;
            // Again, casting T might be problematic. Returning input as a default T for now.
            return (T)input; // Simplification.
        }

        // NOTE: True intersection logic for complex types (especially objects) is very complex.
        // This implementation focuses on the validation aspect. Merging complex structures
        // (like objects with overlapping keys but different types) would require
        // advanced type manipulation, possibly runtime type generation or dynamic objects.
    }
}
```

**Step 2: Add `Zod.Intersection` factory method**
This method will create `IntersectionSchema` instances.

```csharp
// src/ZodSharp.NET.Schema/NamespaceImports.cs (or Zod.cs)
// ... other using statements

public static class Zod
{
    // ... other factory methods

    /// <summary>
    /// Creates an intersection schema that requires values to conform to all provided schemas.
    /// </summary>
    /// <typeparam name="T">The resulting type after parsing. This is often 'object' or a dynamically merged type.</typeparam>
    /// <param name="schemas">The schemas to intersect.</param>
    /// <returns>A new IntersectionSchema.</returns>
    public static IntersectionSchema<T> Intersection<T>(params IZodSchema<object>[] schemas) // Using object input type
    {
        return new IntersectionSchema<T>(schemas);
    }

    // Consider overloads for specific type combinations or object schemas.
    // For object intersection, a specialized method returning a schema for a merged object type would be ideal.
}
```
*Note: The complexity of `IntersectionSchema` lies in merging the results. For object schemas, this means merging properties. For primitive types, it's less relevant unless dealing with unions of primitives where intersection might yield a single type. The current implementation is a simplification focusing on validation.*

**Step 3: Write tests for `IntersectionSchema`**
Test that inputs conforming to all schemas pass, and inputs failing any schema fail.

```bash
# Placeholder for testing IntersectionSchema
# dotnet test --filter "FullyQualifiedName~ZodSharp.NET.Schema.Tests.Unit.Types.Composition.IntersectionSchemaTests"
```

**Step 4: Commit**
```bash
git add src/ZodSharp.NET.Schema/Types/Composition/IntersectionSchema.cs
git add src/ZodSharp.NET.Schema/NamespaceImports.cs # or wherever Zod properties are defined
git commit -m "feat: implement Zod.intersection schema composition"
```

### Task 5: Refactor and Generalize `ObjectSchema` for Composition

**Objective:** Ensure `ObjectSchema` can seamlessly integrate with the new composition types, especially `Extend` and potential future composable schemas.

**Files:**
- Modify: `src/ZodSharp.NET.Schema/Types/ObjectSchema.cs`

**Step 1: Review `ObjectSchema<T>` for composition integration**
Ensure the `ObjectSchema` class correctly handles scenarios where it might be extended or intersected. This may involve:
- Refining the `Extend` method's generic constraints and return type.
- Potentially introducing internal representation of object schemas that better supports merging.
- Considering how `ObjectSchema` instances will be represented within `UNION` or `INTERSECTION` schemas.

**Step 2: Refine `Extend` method's return type and generics**
The current `ObjectSchema<TCombined>` return type with `TCombined : new()` is a placeholder. A more robust solution might involve:
- Using `System.Text.Json.JsonElement` for dynamic object merging.
- Leveraging C#'s experimental `record` types or similar for compile-time merging (if applicable).
- Providing a mechanism to define the `TCombined` type explicitly.

For now, we aim to have `Extend` return a `ZodSchema` that encapsulates the original and extension schemas.

**Step 3: Write tests for `ObjectSchema` compositional behavior**
Test the interaction of `ObjectSchema` with `Extend`.

```bash
# Placeholder for testing ObjectSchema composition
# dotnet test --filter "FullyQualifiedName~ZodSharp.NET.Schema.Tests.Unit.Types.ObjectSchemaCompositionTests"
```

**Step 4: Commit**
```bash
git add src/ZodSharp.NET.Schema/Types/ObjectSchema.cs
git commit -m "refactor: enhance ObjectSchema for composition patterns"
```

### Task 6: Documentation and Examples

**Objective:** Document the new composition features and provide clear examples.

**Files:**
- Create: `docs/features/composition.md`
- Update: `README.md` or other primary documentation entry points.

**Step 1: Create `composition.md`**
Document `z.object().extend()`, `z.union()`, and `z.intersection()`. Include:
- Purpose of each composition type.
- Syntax and C# examples.
- Considerations for type inference and potential issues.

**Step 2: Update `README.md`**
Add a section summarizing the new composition capabilities and link to the detailed documentation.

**Step 3: Add examples to existing tests or a new examples file**
Include comprehensive examples demonstrating the use of composition in practice.

**Step 4: Commit**
```bash
git add docs/features/composition.md
git add README.md
git commit -m "docs: document ZodSharp composition features (extend, union, intersection)"
```

### Task 7: Final Review and Testing

**Objective:** Ensure all new features are robust, well-tested, and integrated correctly.

**Step 1: Run all unit tests**
Ensure all tests pass, including those for the new composition features.
```bash
dotnet test
```

**Step 2: Perform integration testing**
Write or adapt integration tests that use multiple composition patterns together.

**Step 3: Code review**
Conduct a thorough code review of all changes.

**Step 4: Commit**
```bash
git commit -m "feat: finalize composition features with comprehensive testing and review"
```
Link to relevant issues if applicable.
@kieronlanning
The plan is complete and saved to `.hermes/plans/2024-07-12_145000-zodsharp-composition-plan.md`.

Ready to execute using subagent-driven-development — I'll dispatch a fresh subagent per task with two-stage review (spec compliance then code quality). Shall I proceed?