# Source Generator Diagnostics

The `[ZodSchema]` generator ships an analyzer (category `ZodSharp.SourceGenerator`) that reports configuration and usage problems at compile time. All diagnostics below are errors, enabled by default.

| ID | Meaning |
|---|---|
| ZODSGEN001 | Unhandled generator exception (`"Source generator failed for {0}: {1}"`) |
| ZODSGEN003 | Invalid `[Length]` configuration (min > max) |
| ZODSGEN004 | Unsupported `[Length]` target |
| ZODSGEN005 | Invalid DataAnnotations error-message resource configuration (name without type, or type without name) |
| ZODSGEN006 | Unsupported DataAnnotations usage (string-only attributes on non-string targets; `[AllowedValues]`/`[DeniedValues]` on unsupported types; `[RegularExpression]` on non-strings; `[Range]` on unsupported types) |
| ZODSGEN007 | Custom validation method configured but not found (when a name is explicitly configured) |
| ZODSGEN008 | Custom method return type is not `ValueTask<ValidationResult<T>>` |
| ZODSGEN009 | Custom method parameter count is not 2 |
| ZODSGEN010 | First custom method parameter is not the model type |
| ZODSGEN011 | Second custom method parameter is not `CancellationToken` |
| ZODSGEN012 | Custom method is generic |
| ZODSGEN013 | Custom method must be static when defined on the model type |
| ZODSGEN014 | Custom method is inaccessible from the generated validator (private/protected) |
| ZODSGEN015 | Ambiguous custom method overloads (only when at least two valid candidates exist) |
| ZODSGEN016 | Configured method name is not a valid C# identifier |
| ZODSGEN017 | Custom method is abstract |
| ZODSGEN018 | Custom method is an unimplemented partial method |
| ZODSGEN019 | Custom method uses `ref`/`in`/`out`/`params`/`scoped` parameters |
| ZODSGEN020 | `[Compare]` references an unknown property |
| ZODSGEN021 | `System.ComponentModel.DataAnnotations` reference missing |
| ZODSGEN027 | `IValidateOptions` requested but `Microsoft.Extensions.Options` reference is missing |
| ZODSGEN028 | `IValidateOptions` requested on a struct (requires a class) |
| ZODSGEN029 | A model declares both the `OnZodValidate` refinement hook and an async custom validation method (only one is allowed) |
| ZODSGEN030 | A custom rule mapped through `[ZodRule(typeof(...))]` does not implement `IValidationRule<T>` for the property type (including an unbound generic rule that cannot be closed with it) |
| ZODSGEN031 | A custom rule constructor parameter could not be mapped from the attribute (`[ZodRule]`) |
| ZODSGEN032 | A validation attribute could not be generated for a rule marked `[ZodRule]` |
| ZODSGEN033 | (warning) A rule-mapped attribute is applied to a type that gets no generated schema (no `[ZodSchema]` and not reachable as a complex property), so the rule never runs |
| ZODSGEN034 | The `OnZodValidate` refinement hook is implemented on a type that is not `partial` (or whose containing types are not all `partial`), so the generated declaration cannot be emitted |
| ZODSGEN035 | The `OnZodValidate` refinement hook is not declared as `partial void OnZodValidate(RefineCtx<T> context)` (wrong modifiers, return type, or parameters) |
| ZODSGEN036 | A member still uses the retired synchronous refinement contract (`IEnumerable<ValidationError> Validate()`); implement `OnZodValidate` instead |

## Suppressing

Diagnostics can be suppressed per-project or per-site with the standard `#pragma warning disable ZODSGEN006` / `NoWarn` mechanisms. Refer to the analyzer's shipped release notes (`AnalyzerReleases.Shipped.md` / `AnalyzerReleases.Unshipped.md` in the generator project) for the canonical catalog.