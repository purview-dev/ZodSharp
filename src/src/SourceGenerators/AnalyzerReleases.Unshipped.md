; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID | Category | Severity | Notes |
|---|---|---|---|
| ZODSGEN037 | ZodSharp.SourceGenerator | Warning | A rule's derived validation attribute name is already declared by a hand-authored type, so the generated attribute is suppressed in favour of that declaration's own [ZodRule] mapping |
| ZODSGEN038 | ZodSharp.SourceGenerator | Warning | A hand-authored rule attribute's [ZodRule] mapping does not address every rule declared under the name the attribute derives from |
| ZODSGEN039 | ZodSharp.SourceGenerator | Warning | A rule accepts a code/origin constructor parameter without implementing IZodRule, so the value never reaches the reported error identity |
| ZODSGEN040 | ZodSharp.SourceGenerator | Warning | An attribute argument is not consumed by the resolved rule: no matching constructor parameter and not part of the error identity |
| ZODSGEN041 | ZodSharp.SourceGenerator | Info | A typed union whose option types are all reference types can use the native C# 15 union returned by Z.NativeUnion on net11+ (allocation-free, exhaustive pattern matching) |
| ZODSGEN042 | ZodSharp.SourceGenerator | Warning | A validation rule does not expose its error identity as public const ErrorCode/MessageFormat constants, so tests cannot assert against the rule without duplicating literals |
| ZODSGEN043 | ZodSharp.SourceGenerator | Info | A built-in rule marked [ZodRule] does not generate a validation attribute because a constructor parameter cannot be represented as an attribute property (a derived name that collides with System.ComponentModel.DataAnnotations is emitted under a "Zod" suffix instead) |
