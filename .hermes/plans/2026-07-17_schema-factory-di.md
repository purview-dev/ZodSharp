# Schema Factory & Dependency Injection Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Add a DI-friendly schema factory (`IZodSchemaFactory` / `ZodSchemaFactory`) that resolves validators by type at runtime, so both hand-built `IZodSchema<T>` instances and source-generated static `*Schema` classes (e.g. `UserSchema`) participate uniformly.

**Architecture:** Introduce a non-generic `IZodSchemaValidator<T>` abstraction (matching the existing `Examples.CLI/ISchemaValidator<T>` shape) and a registry-backed factory. The factory holds a `ConcurrentDictionary<Type, IZodSchemaValidator>` keyed by validated type. Source-generated schemas register via a generated instance adapter class (`{ClassName}SchemaValidator`) that the factory discovers through a `[module: ZodSchemaGenerated(typeof(T))]` attribute the generator also emits; runtime schemas register explicitly. A `Microsoft.Extensions.DependencyInjection` integration lives in the AspNetCore project (already the host-integration home) via `AddZodSharp(...)` and resolves the factory from the container.

**Tech Stack:** C# 13 / .NET 10, `Microsoft.Extensions.DependencyInjection` (from the ASP.NET Core shared framework), TUnit for tests, Roslyn IIncrementalGenerator for the registration hook.

---

## Current context / assumptions

> **Updated for the refactored source generator structure** (commit `2e15594 refactor: updated source generator to make it more readable/expandable`).

- Core types live in `src/src/ZodSharp/Core/`: `IZodSchema<TOutput,TInput>`, `ZodType<TOutput,TInput>`, `ValidationResult<T>`, `ValidationError`, `ZodException`, `SchemaCache`.
- Source generator (`src/src/SourceGenerators/`) emits `{ClassName}Schema` static partial classes with `Validate(T)` and `Parse(T)` into the target type's namespace. Output file `{ClassName}Schema.g.cs`.
- **Refactored generator structure** (key changes from the old layout):
  - `Helpers/Models/` → `Models/` — all model types now in `ZodSharp.SourceGenerators.Models` namespace (not `...Helpers.Models`).
  - `ExecutionContext` → `GenerationContext` — a `sealed record class` in `Models/GenerationContext.cs`. All `executionContext` parameters are now `generationContext`.
  - `Diagnostic` → `DiagnosticInfo` — generator-internal diagnostics use `DiagnosticInfo` record (in `Models/DiagnosticInfo.cs`). Method signatures take `List<DiagnosticInfo>` not `List<Diagnostic>`.
  - `GeneratorDiagnostics` moved to `Models/GeneratorDiagnostics.cs`.
  - `TargetSymbolDescriptor` moved to `Models/TargetSymbolDescriptor.cs`.
  - `ZodSchemaGenerator.ValueProviders.cs` deleted — value provider logic now in `SourceGenHelpers.GetGeneratorValueProviders`.
  - `ZodSchemaGenerator.cs` `Initialize` now iterates `source.ZodSchemas` (a collection of `GeneratorResult<TargetSymbolDescriptor>`) calling `Execute` per schema.
  - New `Models/GenerationModel.cs`, `Models/GeneratorResult.cs`, `Models/EquatableArray.cs`, `Models/DiagnosticInfo.cs`.
  - `ZodSchemaGenerator.Reporting.cs` — new partial for `ReportDiagnostics` overloads + `ILogSupport.SetLogOutput`.
  - Data attribute models now under `Models/DataAttributes/` namespace `ZodSharp.SourceGenerators.Models.DataAttributes`.
- `Examples.CLI/ISchemaValidator.cs` defines `ISchemaValidator<T>` with `Validate(T)`. We lift this shape into core as the DI contract.
- AspNetCore project (`src/src/AspNetCore/AspNetCore.csproj`) targets `net10.0`, references `ZodSharp.csproj` and `FrameworkReference Microsoft.AspNetCore.App` (which brings `Microsoft.Extensions.DependencyInjection.Abstractions`). It currently only has `ProblemDetailsExtensions.cs`.
- Tests use TUnit (`Assert.That(...).IsEqualTo(...)`, `[Test]`, `[Arguments(...)]`). No FluentAssertions/XUnit/NUnit.
- `Directory.Build.props` sets `TargetFrameworks=netstandard2.1;net10.0` for packable libs; test projects are `net10.0` only.
- CSharpier enforces formatting; `just lint-check` and `just tests` gate changes.
- Source generator test base (`SourceGeneratorTestBase<TGenerator>`) includes the `ZodSharp` assembly reference (line 52: `typeof(Z).Assembly`), so new core types added to `ZodSharp.Core` are automatically resolvable in generated-source compilation tests.
- Test helpers in `ZodSchemaGeneratorTests.cs`: `GetSchemaGeneratedSource(result, schemaName)` finds a generated tree by ` static partial class {schemaName}`; `AssertNoGeneratorExceptions(result)`; `AssertNoCompilationErrors(outputCompilation, ct)`.

**Key design decision:** The generated `*Schema` classes are *static* and have a `Validate(T value)` method with the target's concrete type. They cannot implement an interface directly (static classes can't). So the generator additionally emits a small *instance* adapter class `{ClassName}SchemaValidator : IZodSchemaValidator<{ClassName}>` that delegates to the static `Validate`. The factory registers and resolves these adapters, discovering them via a `[module: ZodSchemaGenerated(typeof({ClassName}))]` attribute the generator also emits.

---

## Proposed approach

1. **Core abstraction** — `IZodSchemaValidator<T>` + `IZodSchemaValidator` (non-generic marker) in `ZodSharp.Core`, mirroring the existing CLI interface but in core so both runtime schemas and generated validators satisfy it.
2. **Runtime adapter** — a `ZodSchemaValidator<T>` that wraps any `IZodSchema<T,T>` (hand-built) into `IZodSchemaValidator<T>`. Lets fluent-API schemas participate in DI.
3. **Factory + registry** — `IZodSchemaFactory` / `ZodSchemaFactory` with `Register<T>(validator)`, `Register(Type, IZodSchemaValidator)`, `TryRegister<T>(validator)`, `Resolve<T>()`, `Validate<T>(value)`, `IsRegistered<T>()`. Thread-safe `ConcurrentDictionary` keyed by `typeof(T)`.
4. **Discovery attribute** — `ZodSchemaGeneratedAttribute` in `ZodSharp.Core`, applied at module level by the generator, enabling the factory to auto-discover generated validators via a single reflection scan.
5. **Source generator hook** — generate `{ClassName}SchemaValidator` instance adapter alongside `{ClassName}Schema` (inside the same `{ClassName}Schema.g.cs` file, after the static class block). Generate a separate `{ClassName}SchemaRegistration.g.cs` file containing only `[module: ZodSchemaGenerated(typeof({ClassName}))]` (module attributes must precede types).
6. **DI integration** — `ZodSharpServiceCollectionExtensions.AddZodSharp(this IServiceCollection, Action<ZodSchemaFactoryOptions>?)` in the AspNetCore project: registers `ZodSchemaFactory` singleton, runs user configuration (register hand-built schemas), and scans calling assemblies for `[module: ZodSchemaGenerated(...)]` attributes to auto-register generated validators.
7. **Tests** — core factory tests in `ZodSharp.UnitTests`; DI extension tests in `AspNetCore.UnitTests` using `ServiceCollection`; generator adapter tests in `SourceGenerators.UnitTests`.

---

## Step-by-step plan

### Task 1: Add `IZodSchemaValidator<T>` and `IZodSchemaValidator` to Core

**Objective:** Define the DI-facing validator contract in `ZodSharp.Core`.

**Files:**
- Create: `src/src/ZodSharp/Core/IZodSchemaValidator.cs`
- Modify: `src/src/ZodSharp/Core/ZodType.cs:168` — make `ZodType<T>` implement `IZodSchemaValidator<T>`
- Test: `src/tests/ZodSharp.UnitTests/Core/IZodSchemaValidatorTests.cs`

**Step 1: Write failing test**

```csharp
using ZodSharp.Core;
using ZodSharp.Schemas;

namespace ZodSharp.UnitTests.Core;

public class IZodSchemaValidatorTests
{
    [Test]
    public async Task Validator_Validate_ReturnsSuccessForValidValue()
    {
        IZodSchemaValidator<string> validator = new ZodString().Min(1);
        var result = validator.Validate("hello");
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task NonGenericMarker_IsImplementedByGenericValidator()
    {
        IZodSchemaValidator validator = new ZodString().Min(1);
        await Assert.That(validator).IsNotNull();
    }
}
```

**Step 2: Run test to verify failure**

Run: `dotnet test src/ZodSharp.slnx --treenode-filter="/*/ZodSharp.UnitTests/*/" --filter "IZodSchemaValidatorTests"`
Expected: FAIL — `IZodSchemaValidator` not found.

**Step 3: Write minimal implementation**

`src/src/ZodSharp/Core/IZodSchemaValidator.cs`:
```csharp
namespace ZodSharp.Core;

/// <summary>
/// Non-generic marker for all Zod schema validators resolved by the DI factory.
/// </summary>
public interface IZodSchemaValidator { }

/// <summary>
/// Type-safe validator contract usable from DI. Mirrors <see cref="IZodSchema{TOutput, TInput}.Validate"/>.
/// </summary>
/// <typeparam name="T">The validated value type.</typeparam>
public interface IZodSchemaValidator<T> : IZodSchemaValidator
{
    /// <summary>Validates <paramref name="value"/> and returns a result.</summary>
    ValidationResult<T> Validate(T value);
}
```

`src/src/ZodSharp/Core/ZodType.cs:168` — add the interface to the convenience base:
```csharp
public abstract class ZodType<T> : ZodType<T, T>, IZodSchema<T>, IZodSchemaValidator<T> { }
```
`Validate(T)` already exists on `ZodType<TOutput,TInput>` with the right signature, so no new method is needed.

**Step 4: Run test to verify pass**

Run: same as Step 2.
Expected: PASS.

**Step 5: Commit**

```bash
git add src/src/ZodSharp/Core/IZodSchemaValidator.cs src/src/ZodSharp/Core/ZodType.cs src/tests/ZodSharp.UnitTests/Core/IZodSchemaValidatorTests.cs
git commit -m "feat: add IZodSchemaValidator<T> core abstraction for DI"
```

---

### Task 2: Add `ZodSchemaValidator<T>` runtime adapter

**Objective:** Wrap any `IZodSchema<T>` into `IZodSchemaValidator<T>` for explicit registration.

**Files:**
- Create: `src/src/ZodSharp/Core/ZodSchemaValidator.cs`
- Test: `src/tests/ZodSharp.UnitTests/Core/ZodSchemaValidatorTests.cs`

**Step 1: Write failing test**

```csharp
using ZodSharp.Core;
using ZodSharp.Schemas;

namespace ZodSharp.UnitTests.Core;

public class ZodSchemaValidatorTests
{
    [Test]
    public async Task Adapter_Delegates_ToInnerSchema()
    {
        IZodSchema<string> inner = new ZodString().Min(3);
        var adapter = new ZodSchemaValidator<string>(inner);
        var ok = adapter.Validate("hello");
        var bad = adapter.Validate("a");
        await Assert.That(ok.IsSuccess).IsTrue();
        await Assert.That(bad.IsSuccess).IsFalse();
    }
}
```

**Step 2: Run test to verify failure**

Run: `dotnet test src/ZodSharp.slnx --treenode-filter="/*/ZodSharp.UnitTests/*/" --filter "ZodSchemaValidatorTests"`
Expected: FAIL — `ZodSchemaValidator<T>` not found.

**Step 3: Write minimal implementation** — `src/src/ZodSharp/Core/ZodSchemaValidator.cs`:

```csharp
namespace ZodSharp.Core;

/// <summary>
/// Wraps an <see cref="IZodSchema{T}"/> as an <see cref="IZodSchemaValidator{T}"/> for DI registration.
/// </summary>
public sealed class ZodSchemaValidator<T>(IZodSchema<T> schema) : IZodSchemaValidator<T>
{
    readonly IZodSchema<T> _schema = schema;

    public ValidationResult<T> Validate(T value) => _schema.Validate(value);
}
```

**Step 4: Run test to verify pass**

Run: same as Step 2.
Expected: PASS.

**Step 5: Commit**

```bash
git add src/src/ZodSharp/Core/ZodSchemaValidator.cs src/tests/ZodSharp.UnitTests/Core/ZodSchemaValidatorTests.cs
git commit -m "feat: add ZodSchemaValidator<T> runtime adapter"
```

---

### Task 3: Add `IZodSchemaFactory` and `ZodSchemaFactory` registry

**Objective:** Thread-safe factory that registers and resolves validators by validated type.

**Files:**
- Create: `src/src/ZodSharp/Core/IZodSchemaFactory.cs`
- Create: `src/src/ZodSharp/Core/ZodSchemaFactory.cs`
- Test: `src/tests/ZodSharp.UnitTests/Core/ZodSchemaFactoryTests.cs`

**Step 1: Write failing test**

```csharp
using ZodSharp.Core;
using ZodSharp.Schemas;

namespace ZodSharp.UnitTests.Core;

public class ZodSchemaFactoryTests
{
    [Test]
    public async Task Register_And_Resolve_Roundtrips()
    {
        var factory = new ZodSchemaFactory();
        factory.Register(new ZodSchemaValidator<string>(new ZodString().Min(2)));
        var validator = factory.Resolve<string>();
        var result = validator.Validate("ok");
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task Resolve_UnregisteredType_Throws()
    {
        var factory = new ZodSchemaFactory();
        await Assert.That(() => factory.Resolve<int>()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Validate_UnregisteredType_Throws()
    {
        var factory = new ZodSchemaFactory();
        await Assert.That(() => factory.Validate(42)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TryRegister_ReturnsFalse_OnDuplicate()
    {
        var factory = new ZodSchemaFactory();
        var v = new ZodSchemaValidator<string>(new ZodString());
        var first = factory.TryRegister(v);
        var second = factory.TryRegister(v);
        await Assert.That(first).IsTrue();
        await Assert.That(second).IsFalse();
    }

    [Test]
    public async Task Register_OverwritesExisting()
    {
        var factory = new ZodSchemaFactory();
        factory.Register(new ZodSchemaValidator<string>(new ZodString().Min(5)));
        factory.Register(new ZodSchemaValidator<string>(new ZodString().Min(1)));
        var result = factory.Validate("a");
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task IsRegistered_UnregisteredType_ReturnsFalse()
    {
        var factory = new ZodSchemaFactory();
        await Assert.That(factory.IsRegistered<int>()).IsFalse();
    }
}
```

> Note: TUnit's `Assert.That(() => expr).Throws<T>()` is the delegate form for synchronous throwing. If TUnit's API differs, use `await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => factory.Resolve<int>()))`. Verify against TUnit docs during implementation.

**Step 2: Run test to verify failure**

Run: `dotnet test src/ZodSharp.slnx --treenode-filter="/*/ZodSharp.UnitTests/*/" --filter "ZodSchemaFactoryTests"`
Expected: FAIL — types not found.

**Step 3: Write minimal implementation**

`src/src/ZodSharp/Core/IZodSchemaFactory.cs`:
```csharp
namespace ZodSharp.Core;

/// <summary>
/// Resolves <see cref="IZodSchemaValidator{T}"/> instances by validated type, for DI scenarios.
/// </summary>
public interface IZodSchemaFactory
{
    /// <summary>Resolves the validator registered for <typeparamref name="T"/>.</summary>
    IZodSchemaValidator<T> Resolve<T>();

    /// <summary>Validates <paramref name="value"/> using the registered validator for <typeparamref name="T"/>.</summary>
    ValidationResult<T> Validate<T>(T value);

    /// <summary>Registers a validator for <typeparamref name="T"/>, overwriting any existing registration.</summary>
    void Register<T>(IZodSchemaValidator<T> validator);

    /// <summary>Registers a non-generic validator instance for <paramref name="targetType"/>, overwriting any existing registration.</summary>
    void Register(Type targetType, IZodSchemaValidator validator);

    /// <summary>Registers only if no validator is already registered; returns false otherwise.</summary>
    bool TryRegister<T>(IZodSchemaValidator<T> validator);

    /// <summary>True if a validator is registered for <typeparamref name="T"/>.</summary>
    bool IsRegistered<T>();
}
```

`src/src/ZodSharp/Core/ZodSchemaFactory.cs`:
```csharp
using System.Collections.Concurrent;

namespace ZodSharp.Core;

/// <summary>
/// Thread-safe default <see cref="IZodSchemaFactory"/> backed by a concurrent dictionary keyed by validated type.
/// </summary>
public sealed class ZodSchemaFactory : IZodSchemaFactory
{
    readonly ConcurrentDictionary<Type, IZodSchemaValidator> _validators = new();

    public IZodSchemaValidator<T> Resolve<T>()
    {
        if (!_validators.TryGetValue(typeof(T), out var validator))
            throw new InvalidOperationException($"No Zod schema validator registered for type '{typeof(T).FullName}'.");

        return (IZodSchemaValidator<T>)validator;
    }

    public ValidationResult<T> Validate<T>(T value) => Resolve<T>().Validate(value);

    public void Register<T>(IZodSchemaValidator<T> validator) => _validators[typeof(T)] = validator;

    public void Register(Type targetType, IZodSchemaValidator validator) => _validators[targetType] = validator;

    public bool TryRegister<T>(IZodSchemaValidator<T> validator) =>
        _validators.TryAdd(typeof(T), validator);

    public bool IsRegistered<T>() => _validators.ContainsKey(typeof(T));
}
```

**Step 4: Run test to verify pass**

Run: same as Step 2.
Expected: PASS.

**Step 5: Commit**

```bash
git add src/src/ZodSharp/Core/IZodSchemaFactory.cs src/src/ZodSharp/Core/ZodSchemaFactory.cs src/tests/ZodSharp.UnitTests/Core/ZodSchemaFactoryTests.cs
git commit -m "feat: add ZodSchemaFactory registry with IZodSchemaFactory"
```

---

### Task 4: Add `ZodSchemaGeneratedAttribute` for generated validator discovery

**Objective:** Provide an assembly/module attribute the source generator emits so the DI integration can auto-discover generated validators via reflection.

**Files:**
- Create: `src/src/ZodSharp/Core/ZodSchemaGeneratedAttribute.cs`
- Test: `src/tests/ZodSharp.UnitTests/Core/ZodSchemaGeneratedAttributeTests.cs`

**Step 1: Write failing test**

```csharp
using System.Reflection;
using ZodSharp.Core;

namespace ZodSharp.UnitTests.Core;

public class ZodSchemaGeneratedAttributeTests
{
    [Test]
    public async Task Attribute_TargetsModule_AndExposesTargetType()
    {
        var attr = new ZodSchemaGeneratedAttribute(typeof(string));
        await Assert.That(attr.TargetType).IsEqualTo(typeof(string));
        await Assert
            .That(typeof(ZodSchemaGeneratedAttribute).GetCustomAttribute<AttributeUsageAttribute>()!.ValidOn)
            .IsEqualTo(AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Assembly);
    }
}
```

**Step 2: Run test to verify failure**

Run: `dotnet test src/ZodSharp.slnx --treenode-filter="/*/ZodSharp.UnitTests/*/" --filter "ZodSchemaGeneratedAttributeTests"`
Expected: FAIL.

**Step 3: Write minimal implementation** — `src/src/ZodSharp/Core/ZodSchemaGeneratedAttribute.cs`:

```csharp
namespace ZodSharp.Core;

/// <summary>
/// Marks a type as having a source-generated Zod schema validator, enabling auto-discovery by <see cref="IZodSchemaFactory"/>.
/// Applied at module level by the <c>ZodSchemaGenerator</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class ZodSchemaGeneratedAttribute : Attribute
{
    /// <summary>The type that has a generated validator.</summary>
    public Type TargetType { get; }

    /// <summary>Initializes a new instance.</summary>
    public ZodSchemaGeneratedAttribute(Type targetType) => TargetType = targetType;
}
```

**Step 4: Run test to verify pass**

Run: same as Step 2.
Expected: PASS.

**Step 5: Commit**

```bash
git add src/src/ZodSharp/Core/ZodSchemaGeneratedAttribute.cs src/tests/ZodSharp.UnitTests/Core/ZodSchemaGeneratedAttributeTests.cs
git commit -m "feat: add ZodSchemaGeneratedAttribute for generator-driven DI discovery"
```

---

### Task 5: Generate `{ClassName}SchemaValidator` instance adapter in source generator

**Objective:** Alongside the static `{ClassName}Schema`, emit an instance class implementing `IZodSchemaValidator<{ClassName}>` that delegates to the static `Validate`.

**Files:**
- Modify: `src/src/SourceGenerators/ZodSchemaGenerator.Execute.cs` — inside `GenerateSchemaClass` (uses `GenerationContext generationContext`, `List<DiagnosticInfo> diagnostics`), after the static class block closes (~line 104, after the `using (generationContext.Writer.Block(...))` closing brace)
- Test: `src/tests/SourceGenerators.UnitTests/ZodSchemaGeneratorTests.ValidatorAdapter.cs`

**Refactored context:** The `GenerateSchemaClass` method now takes `GenerationContext generationContext` and `List<DiagnosticInfo> diagnostics`. The writer is `generationContext.Writer`. The static class block is `using (generationContext.Writer.Block($"{modifier} static partial class {schemaName}")) { ... }` (lines 92-104). The adapter should be emitted immediately after that block closes, still inside `GenerateSchemaClass`, before the `return CodeGenHelpers.ProcessGeneratedCode(...)` at line 106.

**Step 1: Write failing test**

```csharp
namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGeneratorTests
{
    [Test]
    public async Task Generate_GivenZodSchema_AlsoEmitsValidatorAdapter(CancellationToken cancellationToken)
    {
        const string source =
            @"
namespace Testing
{
    [ZodSchema]
    public class Widget
    {
        public string? Name { get; set; }
    }
}";
        var (result, outputCompilation) = await GenerateAsync(source, cancellationToken);
        var generatedSource = GetSchemaGeneratedSource(result, "WidgetSchema");

        await AssertNoGeneratorExceptions(result);
        await AssertNoCompilationErrors(outputCompilation, cancellationToken);
        await Assert.That(generatedSource).Contains("class WidgetSchemaValidator");
        await Assert.That(generatedSource).Contains("IZodSchemaValidator<Widget>");
        await Assert.That(generatedSource).Contains("Validate(Widget value)");
        await Assert.That(generatedSource).Contains("return WidgetSchema.Validate(value);");
    }
}
```

**Step 2: Run test to verify failure**

Run: `dotnet test src/ZodSharp.slnx --treenode-filter="/*/SourceGenerators.UnitTests/*/" --filter "ValidatorAdapter"`
Expected: FAIL — adapter not present in generated source.

**Step 3: Write minimal implementation**

In `src/src/SourceGenerators/ZodSchemaGenerator.Execute.cs`, inside `GenerateSchemaClass`, after the `using (generationContext.Writer.Block($"{modifier} static partial class {schemaName}")) { ... }` block closes (~line 104), add:

```csharp
// After the static schema class block, before the return:
generationContext.Writer.WriteLine();
generationContext.Writer.WriteLine("/// <summary>");
generationContext.Writer.WriteLine($"/// DI-friendly validator adapter for {className}, delegating to {schemaName}.");
generationContext.Writer.WriteLine("/// </summary>");
generationContext.Writer.WriteLine("{{CodeGen}}");
using (generationContext.Writer.Block($"{modifier} sealed class {schemaName}Validator : global::ZodSharp.Core.IZodSchemaValidator<{fullTypeName}>"))
{
    generationContext.Writer.WriteLine($"public global::ZodSharp.Core.ValidationResult<{fullTypeName}> Validate({fullTypeName} value)");
    using (generationContext.Writer.Block())
    {
        generationContext.Writer.WriteLine($"return {schemaName}.Validate(value);");
    }
}
```

Notes:
- The generated file already emits `using ZodSharp.Core;` at line 77, so `IZodSchemaValidator<>` and `ValidationResult<>` are resolvable. Using `global::` prefixes is consistent with existing generated code style.
- `{modifier}` matches the target's accessibility (e.g. `public`, `internal`), already computed at line 57 via `TypeHelpers.GetLimitedAccessibilityKeyword(classSymbol)`.
- `{schemaName}` is `"{className}Schema"` (line 55), so the adapter is `WidgetSchemaValidator`.
- `{fullTypeName}` is `classSymbol.ToDisplayString()` (line 56).
- If `GenerateValidateMethod` is false on the `[ZodSchema]` attribute, skip emitting the adapter (guard with a check of the attribute's `GenerateValidateMethod` property — read it from the `ZodSchemaAttribute` applied to `classSymbol`). For the minimal task, assume the default `true`; a follow-up can add the guard.

**Step 4: Run test to verify pass**

Run: same as Step 2.
Expected: PASS.

**Step 5: Commit**

```bash
git add src/src/SourceGenerators/ZodSchemaGenerator.Execute.cs src/tests/SourceGenerators.UnitTests/ZodSchemaGeneratorTests.ValidatorAdapter.cs
git commit -m "feat(generator): emit IZodSchemaValidator<T> adapter for generated schemas"
```

---

### Task 6: Emit `[module: ZodSchemaGenerated(typeof({ClassName}))]` from source generator

**Objective:** Emit a module-level attribute per generated schema so `AddZodSharp` can discover all generated validators in an assembly via a single reflection scan.

**Files:**
- Modify: `src/src/SourceGenerators/ZodSchemaGenerator.Execute.cs` — in `Execute` (the top-level method, lines 14-45), after `context.AddSource(fileName, ...)` at line 30
- Test: extend `src/tests/SourceGenerators.UnitTests/ZodSchemaGeneratorTests.ValidatorAdapter.cs`

**Refactored context:** The `Execute` method (lines 14-45) now takes `TargetSymbolDescriptor targetDescriptor, GenerationContext generationContext, SourceProductionContext context`. It calls `GenerateSchemaClass` which returns the source string, then `context.AddSource($"{targetDescriptor.Symbol.Name}Schema.g.cs", ...)`. The `fullTypeName` is computed inside `GenerateSchemaClass` (line 56: `classSymbol.ToDisplayString()`), not in `Execute`. To emit the registration file, compute `fullTypeName` in `Execute` from `targetDescriptor.Symbol.ToDisplayString()`.

**Step 1: Write failing test** — append to `ValidatorAdapter.cs`:

```csharp
[Test]
public async Task Generate_GivenZodSchema_EmitsModuleAttribute(CancellationToken cancellationToken)
{
    const string source =
        @"
namespace Testing
{
    [ZodSchema]
    public class Gadget { }
}";
    var (result, outputCompilation) = await GenerateAsync(source, cancellationToken);
    var allGenerated = string.Join("\n", result.GeneratedTrees.Select(t => t.GetText().ToString()));

    await AssertNoGeneratorExceptions(result);
    await AssertNoCompilationErrors(outputCompilation, cancellationToken);
    await Assert.That(allGenerated).Contains("ZodSchemaGenerated");
    await Assert.That(allGenerated).Contains("typeof(Gadget)");
}
```

> The module attribute must be emitted in a separate source file (e.g. `GadgetSchemaRegistration.g.cs`) because module-level attributes must appear before any type declarations in their file. Emit it with `context.AddSource($"{targetDescriptor.Symbol.Name}SchemaRegistration.g.cs", ...)` containing only the `using` directive and the `[module: ZodSchemaGenerated(typeof({fullTypeName}))]` line.

**Step 2: Run test to verify failure**

Run: same filter as Task 5.
Expected: FAIL — `ZodSchemaGenerated` not found.

**Step 3: Write minimal implementation**

In `Execute` (after the existing `context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));` call at line 30), add:

```csharp
// Emit module-level registration attribute for DI discovery
var fullTypeName = targetDescriptor.Symbol.ToDisplayString();
var registrationSource =
    "// <auto-generated />\n"
    + "using ZodSharp.Core;\n\n"
    + $"[module: global::ZodSharp.Core.ZodSchemaGenerated(typeof({fullTypeName}))]\n";
context.AddSource(
    $"{targetDescriptor.Symbol.Name}SchemaRegistration.g.cs",
    SourceText.From(registrationSource, Encoding.UTF8)
);
```

The attribute `ZodSchemaGeneratedAttribute` lives in `ZodSharp.Core` (Task 4), which ships in the `ZodSharp` package. Consuming projects already reference `ZodSharp` (which brings `using ZodSharp.Core;` resolvable). The generator test harness includes the `ZodSharp` assembly reference (line 52 in `SourceGeneratorTestBase`), so the attribute type is resolvable in test compilations.

**Step 4: Run test to verify pass**

Run: same filter.
Expected: PASS.

**Step 5: Commit**

```bash
git add src/src/SourceGenerators/ZodSchemaGenerator.Execute.cs src/tests/SourceGenerators.UnitTests/ZodSchemaGeneratorTests.ValidatorAdapter.cs
git commit -m "feat(generator): emit ZodSchemaGenerated module attribute for DI discovery"
```

---

### Task 7: Add `ZodSchemaFactoryExtensions.RegisterFromAssembly` discovery helper

**Objective:** Core helper that scans an assembly for `[module: ZodSchemaGenerated]` attributes and constructs + registers the corresponding `{ClassName}SchemaValidator` instances on a factory.

**Files:**
- Create: `src/src/ZodSharp/Core/ZodSchemaFactoryExtensions.cs`
- Test: `src/tests/ZodSharp.UnitTests/Core/ZodSchemaFactoryExtensionsTests.cs`

**Step 1: Write failing test**

```csharp
using ZodSharp.Core;

namespace ZodSharp.UnitTests.Core;

// A hand-rolled stand-in for what the source generator would emit,
// so the core helper can be tested without the generator in the loop.
[ZodSchemaGenerated(typeof(SampleDto))]
internal sealed class SampleDtoValidator : IZodSchemaValidator<SampleDto>
{
    public ValidationResult<SampleDto> Validate(SampleDto value) =>
        ValidationResult<SampleDto>.Success(value);
}

internal sealed class SampleDto { public string? Name { get; set; } }

public class ZodSchemaFactoryExtensionsTests
{
    [Test]
    public async Task RegisterFromAssembly_ScansForZodSchemaGenerated_AndRegisters()
    {
        var factory = new ZodSchemaFactory();
        factory.RegisterFromAssembly(typeof(ZodSchemaFactoryExtensionsTests).Assembly);
        await Assert.That(factory.IsRegistered<SampleDto>()).IsTrue();
        var result = factory.Validate(new SampleDto { Name = "x" });
        await Assert.That(result.IsSuccess).IsTrue();
    }
}
```

**Step 2: Run test to verify failure**

Run: `dotnet test src/ZodSharp.slnx --treenode-filter="/*/ZodSharp.UnitTests/*/" --filter "ZodSchemaFactoryExtensionsTests"`
Expected: FAIL — `RegisterFromAssembly` not found.

**Step 3: Write minimal implementation** — `src/src/ZodSharp/Core/ZodSchemaFactoryExtensions.cs`:

```csharp
using System.Reflection;

namespace ZodSharp.Core;

/// <summary>
/// Extension methods for registering validators discovered via <see cref="ZodSchemaGeneratedAttribute"/>.
/// </summary>
public static class ZodSchemaFactoryExtensions
{
    /// <summary>
    /// Scans <paramref name="assembly"/> for <see cref="ZodSchemaGeneratedAttribute"/> and registers
    /// a generated <c>{TypeName}SchemaValidator</c> instance for each target type.
    /// Generated validators are expected to live in the same namespace as the target type and be named
    /// <c>{TypeName}SchemaValidator</c>.
    /// </summary>
    public static IZodSchemaFactory RegisterFromAssembly(this IZodSchemaFactory factory, Assembly assembly)
    {
        foreach (var attr in assembly.GetCustomAttributes<ZodSchemaGeneratedAttribute>())
        {
            var targetType = attr.TargetType;
            var validatorTypeName = $"{targetType.Name}SchemaValidator";
            var validatorType = targetType.Assembly.GetType($"{targetType.Namespace}.{validatorTypeName}")
                ?? throw new InvalidOperationException(
                    $"No generated validator '{validatorTypeName}' found for type '{targetType.FullName}' in assembly '{assembly.GetName().Name}'.");
            var validator = (IZodSchemaValidator)Activator.CreateInstance(validatorType)!;
            factory.Register(targetType, validator);
        }
        return factory;
    }
}
```

Uses the non-generic `Register(Type, IZodSchemaValidator)` method added to `IZodSchemaFactory` / `ZodSchemaFactory` in Task 3 — no reflection-based generic method construction needed.

**Step 4: Run test to verify pass**

Run: same as Step 2.
Expected: PASS.

**Step 5: Commit**

```bash
git add src/src/ZodSharp/Core/ZodSchemaFactoryExtensions.cs src/tests/ZodSharp.UnitTests/Core/ZodSchemaFactoryExtensionsTests.cs
git commit -m "feat: add RegisterFromAssembly discovery for generated validators"
```

---

### Task 8: Add `AddZodSharp` DI integration in AspNetCore project

**Objective:** `IServiceCollection` extension that registers the factory singleton, applies user configuration (register hand-built schemas), and auto-registers generated validators from specified assemblies.

**Files:**
- Create: `src/src/AspNetCore/ZodSchemaFactoryOptions.cs`
- Create: `src/src/AspNetCore/ZodSharpServiceCollectionExtensions.cs`
- Test: `src/tests/AspNetCore.UnitTests/AddZodSharpExtensionsTests.cs`

**Step 1: Write failing test**

```csharp
using Microsoft.Extensions.DependencyInjection;
using ZodSharp.AspNetCore;
using ZodSharp.Core;
using ZodSharp.Schemas;

namespace ZodSharp.AspNetCore;

public class AddZodSharpExtensionsTests
{
    [Test]
    public async Task AddZodSharp_RegistersFactory_AndResolvesRegisteredValidator()
    {
        var services = new ServiceCollection();
        services.AddZodSharp(opts => opts.ConfigureFactory = factory =>
            factory.Register(new ZodSchemaValidator<string>(new ZodString().Min(2))));
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IZodSchemaFactory>();
        var result = factory.Validate("ok");
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task AddZodSharp_AutoRegistersGeneratedValidators_FromConfiguredAssemblies()
    {
        var services = new ServiceCollection();
        services.AddZodSharp(opts => opts.ScanAssemblies.Add(typeof(AddZodSharpExtensionsTests).Assembly));
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IZodSchemaFactory>();
        await Assert.That(factory.IsRegistered<SampleDiDto>()).IsTrue();
    }
}

// Stand-in generated validator for assembly-scan test (the real one is source-generated).
[ZodSchemaGenerated(typeof(SampleDiDto))]
internal sealed class SampleDiDtoSchemaValidator : IZodSchemaValidator<SampleDiDto>
{
    public ValidationResult<SampleDiDto> Validate(SampleDiDto value) => ValidationResult<SampleDiDto>.Success(value);
}

internal sealed class SampleDiDto { public string? Name { get; set; } }
```

**Step 2: Run test to verify failure**

Run: `dotnet test src/ZodSharp.slnx --treenode-filter="/*/AspNetCore.UnitTests/*/" --filter "AddZodSharpExtensionsTests"`
Expected: FAIL — `AddZodSharp` not found.

**Step 3: Write minimal implementation**

`src/src/AspNetCore/ZodSchemaFactoryOptions.cs`:
```csharp
using System.Reflection;
using ZodSharp.Core;

namespace ZodSharp.AspNetCore;

/// <summary>
/// Configuration options for <c>AddZodSharp</c>.
/// </summary>
public sealed class ZodSchemaFactoryOptions
{
    /// <summary>
    /// Assemblies to scan for <see cref="ZodSchemaGeneratedAttribute"/> and auto-register generated validators.
    /// </summary>
    public List<Assembly> ScanAssemblies { get; } = new();

    /// <summary>
    /// Action applied to the factory before the scan, for registering hand-built validators.
    /// </summary>
    public Action<IZodSchemaFactory>? ConfigureFactory { get; set; }
}
```

`src/src/AspNetCore/ZodSharpServiceCollectionExtensions.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using ZodSharp.Core;

namespace ZodSharp.AspNetCore;

/// <summary>
/// DI integration for ZodSharp schema validation.
/// </summary>
public static class ZodSharpServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IZodSchemaFactory"/> as a singleton, applies configuration,
    /// and auto-registers source-generated validators from the configured assemblies.
    /// </summary>
    public static IServiceCollection AddZodSharp(
        this IServiceCollection services,
        Action<ZodSchemaFactoryOptions>? configure = null)
    {
        var options = new ZodSchemaFactoryOptions();
        configure?.Invoke(options);

        services.AddSingleton<IZodSchemaFactory>(sp =>
        {
            var factory = new ZodSchemaFactory();
            options.ConfigureFactory?.Invoke(factory);
            foreach (var assembly in options.ScanAssemblies)
                factory.RegisterFromAssembly(assembly);
            return factory;
        });

        return services;
    }
}
```

> Note: The single-overload design (taking `Action<ZodSchemaFactoryOptions>?`) avoids the overload ambiguity risk noted in the risks section. Inline factory configuration is done via `opts.ConfigureFactory = factory => ...`.

**Step 4: Run test to verify pass**

Run: same as Step 2.
Expected: PASS.

**Step 5: Commit**

```bash
git add src/src/AspNetCore/ZodSchemaFactoryOptions.cs src/src/AspNetCore/ZodSharpServiceCollectionExtensions.cs src/tests/AspNetCore.UnitTests/AddZodSharpExtensionsTests.cs
git commit -m "feat(aspnetcore): add AddZodSharp DI integration with assembly scanning"
```

---

### Task 9: End-to-end test with a source-generated schema participating in DI

**Objective:** Prove the full loop: a class marked `[ZodSchema]` in a test project gets a generated validator + module attribute, and `AddZodSharp` scanning that test assembly registers and resolves it.

**Files:**
- Create: `src/tests/AspNetCore.UnitTests/Fixtures/UserDto.cs` (with `[ZodSchema]`)
- Test: `src/tests/AspNetCore.UnitTests/GeneratedSchemaDITests.cs`
- Potentially modify: `src/tests/AspNetCore.UnitTests/AspNetCore.UnitTests.csproj` (if generator doesn't run transitively)

**Step 1: Write failing test**

`src/tests/AspNetCore.UnitTests/Fixtures/UserDto.cs`:
```csharp
using ZodSharp;

namespace ZodSharp.AspNetCore.Fixtures;

[ZodSchema]
public class UserDto
{
    public string? Name { get; set; }
    public int Age { get; set; }
}
```

`src/tests/AspNetCore.UnitTests/GeneratedSchemaDITests.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using ZodSharp.AspNetCore;
using ZodSharp.AspNetCore.Fixtures;
using ZodSharp.Core;

namespace ZodSharp.AspNetCore;

public class GeneratedSchemaDITests
{
    [Test]
    public async Task AddZodSharp_WithAssemblyScan_RegistersGeneratedUserDtoValidator()
    {
        var services = new ServiceCollection();
        services.AddZodSharp(opts => opts.ScanAssemblies.Add(typeof(UserDto).Assembly));
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IZodSchemaFactory>();

        await Assert.That(factory.IsRegistered<UserDto>()).IsTrue();
        var ok = factory.Validate(new UserDto { Name = "A", Age = 1 });
        await Assert.That(ok.IsSuccess).IsTrue();
    }
}
```

**Step 2: Run test to verify failure**

Run: `dotnet test src/ZodSharp.slnx --treenode-filter="/*/AspNetCore.UnitTests/*/" --filter "GeneratedSchemaDITests"`
Expected: FAIL — either `UserDtoSchemaValidator` not generated (if test project doesn't reference the generator) or registration fails.

> Ensure `AspNetCore.UnitTests.csproj` references the `ZodSharp` project (it does via `ZodSharp.AspNetCore` which references `ZodSharp.csproj`), which pulls in the source generator as an analyzer. Verify the generator runs for the test assembly — the `ZodSharp.csproj` analyzer reference has `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`, so it's available transitively to consumers of `ZodSharp`. Add the fixture, build, and confirm `UserDtoSchemaValidator` + `[module: ZodSchemaGenerated]` appear in the test project's generated files (inspect `obj/.../ZodSharp.SourceGenerators.../`).

**Step 3: Write minimal implementation**

No production code to write — the generator (Tasks 5-6) produces the validator and attribute. If the test fails because the generator isn't running on the test assembly, add an explicit analyzer reference in `AspNetCore.UnitTests.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\AspNetCore\AspNetCore.csproj" />
  <!-- Ensure the source generator runs in the test project so [ZodSchema] fixtures are processed -->
  <ProjectReference Include="..\..\src\SourceGenerators\SourceGenerators.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

> Check the existing `AspNetCore.UnitTests.csproj` — it's currently `<Project Sdk="Microsoft.NET.Sdk"></Project>` (nearly empty). It likely relies on `Directory.Build.props` and implicit transitive analyzer references. If the generator doesn't run, add the explicit reference above. Note: `AspNetCore.UnitTests` is not currently listed in the `.slnx` with a `ProjectReference` to `AspNetCore.csproj` — check whether `Directory.Build.targets` wires this implicitly or whether the csproj needs a `<ProjectReference>` added.

**Step 4: Run test to verify pass**

Run: same as Step 2.
Expected: PASS.

**Step 5: Commit**

```bash
git add src/tests/AspNetCore.UnitTests/Fixtures/UserDto.cs src/tests/AspNetCore.UnitTests/GeneratedSchemaDITests.cs src/tests/AspNetCore.UnitTests/AspNetCore.UnitTests.csproj
git commit -m "test: e2e generated schema participates in DI via AddZodSharp"
```

---

### Task 10: Lint, full test run, and cleanup

**Objective:** Ensure formatting and all tests pass before PR.

**Files:**
- Potentially touched: any files with formatting drift from the new code.

**Step 1: Run formatter check**

Run: `just lint-check`
Expected: PASS (or fix with `just lint-fix` then re-check).

**Step 2: Run all tests**

Run: `just tests`
Expected: All pass.

**Step 3: Fix any issues**

If lint fails: `just lint-fix` then re-run `just lint-check`.
If tests fail: fix root cause, re-run.

**Step 4: Final commit (if any formatting fixes)**

```bash
git add -A
git commit -m "style: apply csharpier formatting to new DI factory code"
```

---

## Files likely to change (summary)

**New (Core):**
- `src/src/ZodSharp/Core/IZodSchemaValidator.cs`
- `src/src/ZodSharp/Core/ZodSchemaValidator.cs`
- `src/src/ZodSharp/Core/IZodSchemaFactory.cs`
- `src/src/ZodSharp/Core/ZodSchemaFactory.cs`
- `src/src/ZodSharp/Core/ZodSchemaGeneratedAttribute.cs`
- `src/src/ZodSharp/Core/ZodSchemaFactoryExtensions.cs`

**New (AspNetCore):**
- `src/src/AspNetCore/ZodSchemaFactoryOptions.cs`
- `src/src/AspNetCore/ZodSharpServiceCollectionExtensions.cs`

**Modified (Source generator):**
- `src/src/SourceGenerators/ZodSchemaGenerator.Execute.cs` — emit adapter class (inside `GenerateSchemaClass`, after static class block) + module attribute (in `Execute`, after `context.AddSource`)

**Modified (Core):**
- `src/src/ZodSharp/Core/ZodType.cs:168` — `ZodType<T>` implements `IZodSchemaValidator<T>`

**New tests:**
- `src/tests/ZodSharp.UnitTests/Core/IZodSchemaValidatorTests.cs`
- `src/tests/ZodSharp.UnitTests/Core/ZodSchemaValidatorTests.cs`
- `src/tests/ZodSharp.UnitTests/Core/ZodSchemaFactoryTests.cs`
- `src/tests/ZodSharp.UnitTests/Core/ZodSchemaGeneratedAttributeTests.cs`
- `src/tests/ZodSharp.UnitTests/Core/ZodSchemaFactoryExtensionsTests.cs`
- `src/tests/SourceGenerators.UnitTests/ZodSchemaGeneratorTests.ValidatorAdapter.cs`
- `src/tests/AspNetCore.UnitTests/AddZodSharpExtensionsTests.cs`
- `src/tests/AspNetCore.UnitTests/Fixtures/UserDto.cs`
- `src/tests/AspNetCore.UnitTests/GeneratedSchemaDITests.cs`

---

## Tests / validation

- `dotnet build src/ZodSharp.slnx` — compiles with no errors/warnings (new code).
- `dotnet test src/ZodSharp.slnx --treenode-filter="/*/*/*/*/"` — all unit + integration tests pass.
- `just lint-check` — CSharpier-clean.
- Manual sanity: confirm a `[ZodSchema] public class User` in a consuming project produces `UserSchemaValidator` + `[module: ZodSchemaGenerated(typeof(User))]` in the generated files (inspect `obj/.../generated/`).

---

## Risks, tradeoffs, and open questions

1. **Static classes can't implement interfaces.** The generated `{ClassName}Schema` is `static partial class`, so it can't satisfy `IZodSchemaValidator<T>` directly. The plan generates a *separate instance* adapter (`{ClassName}SchemaValidator`). Trade-off: an extra type per schema + one instance allocation on resolve. Acceptable for DI scenarios; the static `Validate` remains the zero-alloc hot path.

2. **Module attribute placement.** Module-level attributes must be at the top of a source file, before types. The plan emits a *separate* `{ClassName}SchemaRegistration.g.cs` file with only the attribute to avoid conflicts with the schema file (which already has a namespace + class). Verify Roslyn accepts multiple module attributes across separate generated files (it does — each file is independent).

3. **Assembly scanning cost.** `RegisterFromAssembly` reflects once per assembly at `AddZodSharp` time. For large assemblies this is a one-time startup cost. Trade-off: automatic registration vs. explicit `factory.Register(new FooSchemaValidator())`. Both are supported; scanning is opt-in via `opts.ScanAssemblies`.

4. **Internal types & accessibility.** If the target class is `internal`, the generated `*SchemaValidator` is also `internal`. The `RegisterFromAssembly` reflection (`Activator.CreateInstance`) works on internal types within the same assembly. Cross-assembly internal schemas would need `InternalsVisibleTo`. Document this.

5. **`ZodType<T>` now implements `IZodSchemaValidator<T>`.** This is a binary-compatible addition (new interface on an existing abstract base). Verify no existing tests assume `ZodType<T>` implements *only* `IZodSchema<T>` — unlikely but check during Task 1.

6. **Generator runs on `netstandard2.0`.** The adapter references `ZodSharp.Core` types via `global::ZodSharp.Core.IZodSchemaValidator<>`. The generator itself doesn't need the `ZodSharp` assembly at compile time (it emits strings), but the *generated code* is compiled in the consuming project which references `ZodSharp`. The generator test harness (`SourceGeneratorTestBase`) includes the `ZodSharp` assembly reference (line 52: `typeof(Z).Assembly`). The new `IZodSchemaValidator` type must be resolvable in the test compilation, which it will be since it's in `ZodSharp.Core` within the same `ZodSharp` assembly.

7. **Refactored generator structure.** The generator was refactored in commit `2e15594`. Key renames: `ExecutionContext` → `GenerationContext`, `Diagnostic` → `DiagnosticInfo`, `Helpers/Models/` → `Models/`. All code snippets in Tasks 5-6 use the new names (`generationContext`, `DiagnosticInfo`, `Models/`). The `Execute` method signature is `Execute(TargetSymbolDescriptor, GenerationContext, SourceProductionContext)`. The `GenerateSchemaClass` method signature is `GenerateSchemaClass(INamedTypeSymbol, GenerationContext, List<DiagnosticInfo>)`.

8. **Open question — should `IZodSchemaValidator<T>` also expose `ValidateAsync`?** The current `IZodSchema<T>` has `ValidateAsync`. For parity, add `ValueTask<ValidationResult<T>> ValidateAsync(T value)` to `IZodSchemaValidator<T>`. Decide during Task 1; recommend yes for future-proofing DI consumers that want async.

9. **Open question — `Examples.CLI/ISchemaValidator.cs`.** Now that core has `IZodSchemaValidator<T>`, the CLI interface is redundant. Consider migrating `Examples.CLI` to use the core interface (out of scope for this plan, but note it).

10. **TUnit exact exception-assertion API.** The plan uses `Assert.That(() => expr).Throws<T>()` (delegate form). Verify TUnit supports this; if not, use `await Assert.ThrowsAsync<T>(() => Task.Run(() => expr))`. Check during Task 3 implementation and standardize across tests.

11. **`AspNetCore.UnitTests` project references.** The test project csproj is currently `<Project Sdk="Microsoft.NET.Sdk"></Project>` (nearly empty). It may need an explicit `<ProjectReference>` to `AspNetCore.csproj` and possibly an explicit analyzer `<ProjectReference>` to `SourceGenerators.csproj` (Task 9). Verify during Task 9.

