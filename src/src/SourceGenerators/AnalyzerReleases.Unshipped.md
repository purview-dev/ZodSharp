; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID | Category | Severity | Notes |
|---|---|---|---|
| ZODSGEN037 | ZodSharp.SourceGenerator | Warning | A rule's derived validation attribute name is already declared by a hand-authored type, so the generated attribute is suppressed in favour of that declaration's own [ZodRule] mapping |
| ZODSGEN038 | ZodSharp.SourceGenerator | Warning | A hand-authored rule attribute's [ZodRule] mapping does not address every rule declared under the name the attribute derives from |
| ZODSGEN039 | ZodSharp.SourceGenerator | Warning | A rule accepts a code/origin constructor parameter without implementing IZodRule, so the value never reaches the reported error identity |
| ZODSGEN040 | ZodSharp.SourceGenerator | Warning | An attribute argument is not consumed by the resolved rule: no matching constructor parameter and not part of the error identity |
