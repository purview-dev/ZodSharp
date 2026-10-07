# Changelog

All notable changes to this repository are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). `package.json` is the authoritative version; see
[Release Flow](docs/wiki/Release-Flow.md).

Released versions correspond to `v<version>` GitHub releases. Entries below the `Unreleased` heading have not
been published to NuGet.

## Unreleased

> **This release contains breaking changes.** See [Breaking changes](#breaking-changes) below before
> upgrading from `2.0.0`. The analyzer catalogue records them under `2.1.0`; `package.json` still carries the
> prerelease version and is set at release time.
>
> Note the changes below are consumer-visible: a rule's public `Code` constant was renamed to `ErrorCode`,
> and rule constructor signatures changed, so attribute usages and code referencing those constants may not
> compile against this version. Read the migration notes before upgrading.

### Breaking changes

- **Rule error-identity constants renamed from `Code` to `ErrorCode`.** Every validation rule now exposes its
  error identity as public `const string ErrorCode` and `const string MessageFormat`. Code that referenced a
  rule's `Code` constant must use `ErrorCode`. The new `ZODSGEN042` analyzer enforces the convention on
  source-declared rules.
- **Rule constructors are standardised.** Every rule now has a consistent constructor, extension method and
  generated attribute. Generated rule attributes carry the rule's *value* parameters as constructor arguments:
  a parameter declared without a default becomes a required positional argument, a parameter with a default
  keeps it, and `message`/`code`/`origin` remain properties. Attribute usages for required values become
  positional — `[MinValue(3)]`, `[Regex("^[a-z]+$")]`. Usages that previously set a required value by property
  name no longer compile.

### Added

- **Native C# 15 union support.** `Z.NativeUnion` and `ZodTypedNativeUnion` return a native union on `net11.0`,
  giving allocation-free, exhaustively pattern-matchable options. `ZODSGEN041` suggests it where a typed union's
  option types are all reference types.
- `net11.0` added to the target frameworks (now `net8.0`, `net9.0`, `net10.0`, `net11.0`).
- **Per-framework `Microsoft.Extensions.*` dependencies.** All target frameworks previously resolved
  `Microsoft.Extensions.Options` and `…DependencyInjection.Abstractions` 10.0.12, so referencing this package
  dragged a **net8.0 (LTS)** application onto .NET 10 assemblies. Each framework now gets its matching line:
  net8.0 → 8.0.x, net9.0 → 9.0.20, net10.0 → 10.0.12. On net11.0 the SDK prunes them entirely because the
  targeted framework supplies them, which is why the net11.0 dependency group in the `.nuspec` is empty —
  that is expected, and a net11.0 consumer resolves them from the framework (verified).
- **Auto-generated attributes for all rule types**, including non-generic and open-generic rules under a single
  attribute, and expanded downstream rule generation for `Purview.ValueObjects` consumers.
- A new `enum` rule, applied automatically on generated schemas, plus additional explicit rules.
- `IZodRule` organisation with a supporting analyzer, and `RuleMessage` is now public.
- Seven new diagnostics: `ZODSGEN037`–`ZODSGEN043`, recorded in `AnalyzerReleases.Shipped.md` under
  `## Release 2.1.0`. See [Source Generator Diagnostics](docs/wiki/Source-Generator-Diagnostics.md).
  `AnalyzerReleases.Unshipped.md` is now empty; the next diagnostic added goes there and moves across when
  it ships.

### Changed — trimming and Native AOT

- `Purview.ZodSharp` is now marked `IsAotCompatible`, enabling `IsTrimmable` and both analyzers. Getting
  there meant replacing the JSON Schema exporter's name-based reflection with type-checked internal seams
  (`IJsonSchemaArrayInfo`, `IJsonSchemaInnerSchema`, `IJsonSchemaNullableInfo`, `IJsonSchemaLiteralInfo`,
  plus internal accessors on the rules and `ZodType`). That removed all 17 trim warnings *and* the six
  silent export defects above — both were symptoms of the same root cause.
- **`RegisterFromAssembly` is now genuinely trim- and AOT-safe, not merely annotated.** It used to rebuild
  the validator's type name as a string and ask `Assembly.GetType(string)` for it — untrimmable, so the
  validators were removed from a published application and registration threw at runtime. The generator now
  records the validator in the attribute as a `typeof`, which roots it, and
  `ZodSchemaGeneratedAttribute.ValidatorType` is annotated
  `[DynamicallyAccessedMembers(PublicParameterlessConstructor)]` so its constructor survives. No string
  resolution remains, and the `[RequiresUnreferencedCode]` annotation has been removed entirely. Discovery
  is also now proportional to the number of generated schemas rather than the size of the assembly.
  - **Breaking:** `ZodSchemaGeneratedAttribute` now takes `(Type targetType, Type validatorType)`. The
    attribute is generator-emitted, so it regenerates on rebuild; only hand-written usages need updating.
- **`Purview.ZodSharp.SystemTextJson` is AOT-clean and marked.**
  - The validating `JsonConverter<T>` resolves a `JsonTypeInfo<T>` from the serializer options instead of
    calling the reflection-based `JsonSerializer` overloads, so it defers to whatever resolver the host
    configured — a source-generated `JsonSerializerContext` in a trimmed or AOT application. It also caches
    the converter-excluding options copy per instance; it previously rebuilt them on every read and write,
    which meant a cold metadata cache on each call, not just an allocation.
  - Every extension method now comes in a pair: a `JsonTypeInfo<T>` overload that is safe everywhere, and
    the existing `JsonSerializerOptions` overload annotated `[RequiresUnreferencedCode]`/
    `[RequiresDynamicCode]`. This is the same shape `JsonSerializer` itself uses.
  - JSON Schema import and export use this package's own source-generated `JsonSchemaJsonContext`, so they
    need nothing from the consumer. To make that possible the custom `JsonSchemaNamingPolicy` was removed —
    a naming policy is a runtime object the source generator cannot reproduce — and the four `$`-prefixed
    keyword names are now declared with `[JsonPropertyName]` on `JsonSchemaDefinition`. **The wire format is
    unchanged**, and the test for it now asserts the emitted JSON rather than the naming mechanism.
- `Purview.ZodSharp.AspNetCore` is AOT-clean and marked. Graph-based assembly scanning
  (`ScanAssemblyGraphs`) stays reflective by nature and is documented as the path to avoid in a trimmed or
  AOT host, in favour of naming assemblies explicitly or registering validators directly.
- The discriminated-union discriminator accessor falls back to plain reflection when
  `RuntimeFeature.IsDynamicCodeSupported` is false, instead of relying on expression compilation that Native
  AOT does not provide.
- `Purview.ZodSharp.NewtonsoftJson` is **not** marked AOT-compatible and cannot be: Newtonsoft.Json is
  reflection-based throughout. Use `Purview.ZodSharp.SystemTextJson` in a trimmed or Native AOT
  application.

### Changed

- Clarified in `SourceGenerators.csproj` that `EnforceExtendedAnalyzerRules` and `TreatWarningsAsErrors` are
  supplied and enforced by `Purview.BuildSdk` for every `IsRoslynComponent` project, rather than being optional
  project settings. Both were already `true`; the commented-out block implied otherwise.

### Security

- **Regex match timeouts on every pattern the library compiles.** `RegexRule(string)` and the generated
  `[RegularExpression]`/`[Regex]` support previously compiled patterns with **no** timeout, so a
  catastrophically backtracking pattern could hang the calling thread — and the generated path, which is how
  ASP.NET Core request DTOs are validated, dropped the 2-second default that
  `System.ComponentModel.DataAnnotations.RegularExpressionAttribute` provides. JSON Schema import was worse:
  both the pattern and the input come from outside the application. All of these now carry
  `RegexRule.DefaultMatchTimeout`, and exceeding it is reported as a validation failure rather than an
  exception escaping into the host.
- The shared budget is 2 seconds, matching `RegularExpressionAttribute`. The built-in `Email`, `Url` and
  `Duration` rules previously used 100 ms, which proved too tight: under full-suite parallel load their
  matches exceeded it and threw `RegexMatchTimeoutException`, rejecting valid input. Bounding the work is
  what defeats ReDoS; an aggressive bound only adds false negatives.

### Fixed

- **`[RequiredZod]` now rejects an absent value exactly like `[Required]`.** A custom rule bound to a
  reference-type member was emitted inside the generator's non-null guard, so a missing value never reached
  `RequiredRule<T>` and options/schema validation passed. The rule now implements the new `IRequiredRule`
  marker and the generator emits it before the guard, reporting `missing_field` for `null` (and, unless
  allowed, empty or whitespace-only strings). The attribute's `AllowEmptyString` property was renamed to
  `AllowEmptyStrings` to match `RequiredAttribute.AllowEmptyStrings`, so `[RequiredZod]` is a drop-in
  replacement for `[Required]`.
- **`ZodArray` built corrupt error paths for nested element failures.** It used the two-argument
  `ImmutableArray.CopyTo(destination, destinationIndex)` — which copies *into* that index — and then
  overwrote the copied element with the index segment. For an element error at path `["email"]` the result
  was `[null, "[0]"]` instead of `["[0]", "email"]`: the field name was destroyed and a null segment
  introduced. `ProblemDetails` therefore reported the key as `[0]`, so an API client could not tell which
  field of which array element failed. Only arrays whose element schema produces a path (arrays of objects,
  nested arrays) were affected, which is why the existing containment-based assertion did not catch it.
- **`ZodArray` enforces its length constraints before validating any element.** They depend only on the
  array's length, but were checked last — so an array of a million elements against `.Max(10)` ran a
  million element validations and allocated a million `ValidationError`s, each with an interpolated index
  string, before reporting that the array was simply too long. A single request could pin a core and a
  large heap allocation on input the schema had already declared out of range. Note this changes which
  error is reported when an array is both the wrong length *and* has invalid elements: the length failure
  now wins, which matches the container constraint being the outer one.
- **`CompiledValidator` no longer leaks a `DynamicMethod` per call.** It built an expression tree on every
  invocation that compiled to a single `IZodSchema.Validate` call — no inlining, no devirtualisation, just a
  `LambdaExpression.Compile` whose emitted method is never reclaimed in a non-collectible load context. The
  documented inline usage therefore leaked managed and native memory for the process lifetime. The delegate
  now binds `Validate` directly and is cached per schema instance with weak keys, which is behaviourally
  identical, strictly cheaper, and removes the `RequiresDynamicCode` dependency.
- **JSON Schema export was silently dropping most of the schema.** `ToJsonSchemaConverter` reached into the
  library's own types by reflecting on type and field *names*, and several of those names matched nothing.
  `GetField` returns null rather than throwing, so each lookup failed quietly. Fixed, with tests:
  - **Object properties were always empty.** It looked for a field `_shape` on `ZodObject`; the shape is a
    primary-constructor parameter and `ZodObject.Shape` has been public all along.
  - **Object property types were empty even once the shape was found**, because the builders wrap each field
    schema to present it untyped and the wrapper fell through to the generic fallback.
  - **Array `Min`/`Max` never appeared.** It looked for rules named `MinItemsRule`/`MaxItemsRule` reading
    fields `_minItems`/`_maxItems`. None of those four names exists anywhere in the library.
  - **Array element schemas never appeared**, from a `_elementSchema` field that does not exist.
  - **Optional properties exported as an empty "any" schema**, from a `_innerSchema` field that does not exist.
  - **Unions exported with an empty `anyOf`**, from a `_options` field that does not exist — and the cast
    expected an array where the member is an `IReadOnlyList`.
  - **Optional properties were still listed as required**, because the required check matched the wrapper's
    type name rather than asking `IOptionalSchema`.
- Scalar schema generation, which had regressed.
- The `uuid` attribute rule required both constructors to be present to generate.
- Nullability annotations on `IZodRule` properties.
- **A scalar rule adapted over a nullable reference value no longer emits a nullability-mismatched adapter.**
  `CustomRuleResolver` built the `ScalarRuleAdapter<TSelf, TValue, TRule>` identity from a `TypeIdentity`,
  which drops nullable reference annotations, while the wrapped rule was closed over a `TypeReference` that
  keeps them. A scalar backed by `string?` therefore emitted
  `ScalarRuleAdapter<T, string, NonSentinelRule<string?>>`, whose `TValue` satisfied neither the adapter's
  `IValidationRule<TValue>` constraint nor the value object's `IScalarValueObject<TSelf, TValue>` — reporting
  CS8631 at every consumer. The value argument is now carried as a `TypeReference`, so both sides stay in step.

### Added — packed generator smoke test

- `just smoke-packed-generator` (and a `packed-generator` CI job) packs the package, then builds and runs a
  throwaway consumer against it with an isolated package cache. Every in-repo generator test runs against
  the **unmerged** generator by design, and pack validation only checks the IL-merged assembly is *present*
  in the `.nupkg` — nothing checked that it loads and generates. That merge has regressed twice (`#36`,
  `#38`), both times silently breaking consumers while this repository's suite stayed green. Verified to
  have teeth: with generation disabled the consumer fails to compile with
  `CS0103: The name 'PersonSchema' does not exist`.

### Documentation

- **Removed two unsubstantiated performance claims.** "10x faster than reflection-based validation" appeared
  twice; the benchmark suite measures this library against itself across scenarios and contains no
  comparison with another validation library, so there was nothing behind it. The README now says so
  explicitly and points you at measuring your own schemas. Also removed "Array pooling via `ArrayPool<T>`
  for zero-allocation helpers": `ArrayPool` appears only inside `ZeroAllocationHelpers`, which is
  `internal` and has no callers anywhere — the claim described dead code. The `Span<T>` bullet now names
  where that work actually is (`IStringValidationRule`, `ZodString.ValidateSpan`/`IsValidSpan`,
  `EmojiRule`). The measured sub-microsecond timings are unchanged; those are benchmark-backed.
- **Fixed a thread-safety contradiction.** `Compiled-Validators-and-Caching.md` claimed "Schemas are
  immutable and shareable", while `Guarantees-and-Limitations.md` correctly documents that the fluent rule
  methods mutate the receiver in place. A reader who believed the first would cache a schema and then
  mutate it from a request path. That page now states the build-then-share rule, cross-references the
  limitation, and documents three properties of `SchemaCache` that matter because it is process-global: a
  shared key space, no type check on retrieval, and no eviction.
- `Compiled-Validators-and-Caching.md` also described the old expression-tree implementation and claimed it
  removed interface dispatch. Neither was true: the tree was a single `Validate` call. The page now
  describes what the type actually does — and says plainly that it does not make validation faster.
- `Performance.md` records that its figures were produced with BenchmarkDotNet 0.15.8 while the repository
  now pins 0.16.0-preview.2, so the numbers are from a different version than the suite builds against.
- Corrected the target-framework list in `README.md` and linked `LICENSE.md` from the licence section.
- The vitest cross-platform suite now runs in CI, and it **fails** rather than silently passing when the C#
  cross-platform fixtures are absent. It also now reads the C# output recursively — the previous flat directory
  read never matched a file, so the C#-to-Zod handshake had never actually been asserted.

## 2.0.0

First stable release. See the
[`v2.0.0`](https://github.com/purview-dev/zodsharp/releases/tag/v2.0.0) release notes, and
[What's new in v2](README.md#whats-new-in-v2) for the summary of the `Purview.*` package IDs, the JSON
integrations, JSON Schema interoperability, the ASP.NET Core `ProblemDetails` integration and the expanded
DataAnnotations support.
