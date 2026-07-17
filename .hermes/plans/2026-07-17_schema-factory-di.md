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
