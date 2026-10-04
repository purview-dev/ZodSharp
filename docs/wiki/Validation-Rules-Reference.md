# Validation Rules Reference

Every built-in validation rule lives in the `ZodSharp.Rules` namespace and is a `readonly record struct` implementing `ZodSharp.Core.IValidationRule<T>`. Rules are the smallest unit of validation: a schema is a type/structural check followed by an ordered list of rules, and each failing rule adds a `ValidationError` to the result.

This page is the catalogue of the rules that ship with the core package. For authoring your own, see [Custom Rules](Custom-Rules.md).

## The rule contract

```csharp
namespace ZodSharp.Core;

public interface IValidationRule<T>
{
    bool IsValid(in T value);
    string GetErrorMessage(in T value);
    string Code => "validation_failed";
}
```

- String rules additionally implement `ZodSharp.Core.IStringValidationRule` (`bool IsValid(ReadOnlySpan<char>)` / `string GetErrorMessage(ReadOnlySpan<char>)`) so `ZodString.ValidateSpan`/`IsValidSpan` can validate without materialising the input.
- Every rule exposes its error identity as public constants: `public const string ErrorCode` and `public const string MessageFormat` (a `{0}`-style `string.Format` template). `Code` returns `ErrorCode`, and `GetErrorMessage` formats `MessageFormat` with `RuleMessage.Format` (which caches the parsed `CompositeFormat`).
- A constructor `message` parameter, when supplied, overrides the formatted `MessageFormat`. `MessageFormat` is the fallback, not the only message.
- The analyzer reports `ZODSGEN042` when a source-declared rule omits `ErrorCode`/`MessageFormat`. See [Source Generator Diagnostics](Source-Generator-Diagnostics.md).

Rules can be used three ways:

```csharp
using ZodSharp;
using ZodSharp.Rules;

// 1. Standalone
var rule = new EmailRule();
if (!rule.IsValid("not-an-email"))
    Console.WriteLine(rule.GetErrorMessage("not-an-email"));

// 2. Attached to a schema (AddRule or the generic Rule helper)
var schema = Z.String().AddRule(new EmailRule());
var same = Z.String().Rule(new EmailRule());

// 3. Surfaced as a DataAnnotations attribute the [ZodSchema] generator honours
//    (built-in mappings, or a custom rule via [ZodRule]). See Source Generator DataAnnotations.
```

Tests can assert against the rule's constants instead of duplicating literals:

```csharp
var result = Z.String().Email().Validate("not-an-email");
await Assert.That(result.Errors[0].Code).IsEqualTo(EmailRule.ErrorCode);          // "invalid_string"
await Assert.That(result.Errors[0].Message).IsEqualTo(
    string.Format(CultureInfo.CurrentCulture, EmailRule.MessageFormat, "not-an-email"));
```

## Error codes

The built-in rules report one of the following Zod-compatible codes:

| Code | Meaning | Emitted by |
|---|---|---|
| `invalid_string` | A string did not match the required format | all string format rules |
| `invalid_type` | A value was not the expected type/shape (e.g. not a whole number) | `IntRule` |
| `too_small` | A length or value fell below a lower bound | `MinLengthRule`, `MinValueRule<T>`, `GreaterThanRule<T>` |
| `too_big` | A length or value exceeded an upper bound | `MaxLengthRule`, `MaxValueRule<T>`, `LessThanRule<T>`, `SafeIntegerRule` |
| `not_multiple_of` | A number was not a multiple of the divisor | `MultipleOfRule` |
| `not_finite` | A number was `NaN` or infinite | `FiniteRule` |
| `invalid_value` | A value was a rejected sentinel | `NonSentinelRule<T>` |
| `validation_failed` | Fallback for rules that do not declare an identity | any rule without `ErrorCode`/`IZodRule` |

## String rules

Fluent methods live on `ZodString` (`ZodSharp.Schemas`). The `Span` column marks rules that implement `IStringValidationRule` and therefore stay on the zero-allocation span path.

### Length

| Rule | Constructor | Fluent method | Code | Message format | Span |
|---|---|---|---|---|---|
| `MinLengthRule` | `(int minLength)` | `Min(n)`, `Length(n)` | `too_small` | `String must be at least {0} characters long, but got {1}` | yes |
| `MaxLengthRule` | `(int maxLength)` | `Max(n)`, `Length(n)` | `too_big` | `String must be at most {0} characters long, but got {1}` | yes |

`Z.String().Length(n)` adds **both** rules, so the string must be exactly `n` characters.

### Format

| Rule | Constructor | Fluent method | Code | Message format | Span |
|---|---|---|---|---|---|
| `EmailRule` | `()` | `Email()` | `invalid_string` | `Invalid email format: {0}` | yes |
| `RegexRule` | `(Regex pattern, string? message)` or `(string pattern, string? message)` | `Regex(...)` | `invalid_string` | `String does not match the required pattern: {0}` | yes |
| `UrlRule` | `(string? message)` | `Url()` | `invalid_string` | `Invalid URL format: {0}` | no |
| `UriRule` | `(UriKind uriKind, string? message)` | `Uri()`, `Uri(uriKind)` | `invalid_string` | `Invalid Uri, kind: {0}, format: {1}` | no |
| `PhoneRule` | `(string? message)` | `Phone()` | `invalid_string` | `Invalid phone number format: {0}` | yes |
| `E164Rule` | `(string? message)` | `E164()` | `invalid_string` | `Invalid E.164 phone number: {0}` | yes |
| `CreditCardRule` | `(string? message)` | `CreditCard()` | `invalid_string` | `Invalid credit card number format: {0}` | yes |
| `Base64StringRule` | `(string? message)` | `Base64String()` | `invalid_string` | `Invalid Base64 string format: {0}` | no |
| `Base64UrlRule` | `(string? message)` | `Base64Url()` | `invalid_string` | `Invalid base64url string: {0}` | yes |
| `UUIDRule` | `(string? message)` or `(UuidVersion version, string? message)` | `UUID()` / `UUID(version)` | `invalid_string` | `Invalid UUID format: {0}` (or `Invalid UUID v{0} format: {1}`) | yes |
| `ULIDRule` | `(string? message)` | `ULID()` | `invalid_string` | `Invalid ULID: {0}` | yes |
| `JWTRule` | `(string? message)` | `JWT()` | `invalid_string` | `Invalid JWT: {0}` | yes |
| `IPAddressRule` | `(string? message)` | `IP()` | `invalid_string` | `Invalid IP address: {0}` | yes |
| `HexRule` | `(string? message)` | `Hex()` | `invalid_string` | `Invalid hexadecimal string: {0}` | yes |
| `DateStringRule` | `(string? message)` | `Date()` | `invalid_string` | `Invalid date: {0}` | yes |
| `TimeStringRule` | `(string? message)` | `Time()` | `invalid_string` | `Invalid time: {0}` | yes |
| `DatetimeStringRule` | `(string? message)` | `Datetime()` | `invalid_string` | `Invalid date-time: {0}` | yes |
| `NanoidRule` | `(string? message)` | `Nanoid()` | `invalid_string` | `Invalid nanoid: {0}` | yes |
| `Cuid2Rule` | `(string? message)` | `Cuid2()` | `invalid_string` | `Invalid CUID2: {0}` | yes |
| `StartsWithRule` | `(string prefix, string? message)` | `StartsWith(prefix)` | `invalid_string` | `String must start with '{0}', but got '{1}'` | yes |
| `EndsWithRule` | `(string suffix, string? message)` | `EndsWith(suffix)` | `invalid_string` | `String must end with '{0}', but got '{1}'` | yes |
| `IncludesRule` | `(string substring, string? message)` | `Includes(substring)` | `invalid_string` | `String must contain '{0}', but got '{1}'` | yes |

Behaviour notes:

- **`EmailRule`** matches a compiled, case-insensitive regex (`^[^@\s]+@[^@\s]+\.[^@\s]+$`).
- **`RegexRule`** uses the supplied `Regex`; the `Z.String().Regex(string)` overload compiles the pattern with a 100 ms timeout. Its message's `{0}` is the pattern, not the value.
- **`UrlRule`** accepts the compiled HTTP(S) regex **or** an absolute `http`/`https` URI (`Uri.TryCreate`), so it may allocate when the fast regex misses.
- **`UriRule`** validates with `Uri.TryCreate` against the supplied `UriKind` and therefore allocates. `Z.String().Uri()` defaults to `UriKind.RelativeOrAbsolute`; pass an explicit `UriKind` to require an absolute or relative URI.
- **`PhoneRule`** mirrors `[Phone]`: digits plus `() .+-`, with at least one digit.
- **`E164Rule`** requires `+`, a first digit `1`-`9`, and 7-15 digits in total.
- **`CreditCardRule`** mirrors `[CreditCard]`: Luhn check, ignoring spaces and hyphens.
- **`Base64StringRule`** mirrors `[Base64String]`: `Convert.FromBase64String` must succeed.
- **`Base64UrlRule`** accepts URL-safe base64 (`A-Z a-z 0-9 - _`) with no padding; a length of `4n+1` is rejected.
- **`UUIDRule`** accepts RFC 9562 versions 1-8 with a variant nibble of `8`-`9`/`a`-`b`, plus the nil and max UUIDs. The versioned constructor requires a specific version and rejects nil/max.
- **`ULIDRule`** requires 26 Crockford base32 characters, with the first character `0`-`7`.
- **`JWTRule`** requires three non-empty base64url-encoded segments separated by periods.
- **`IPAddressRule`** accepts IPv4 or IPv6 (`IPAddress.TryParse`).
- **`HexRule`** accepts any run of ASCII hex digits; the empty string is valid, matching Zod.
- **`DateStringRule`** parses `yyyy-MM-dd` (invariant culture); **`TimeStringRule`** accepts `HH:mm`, optionally `:ss` and fractional seconds; **`DatetimeStringRule`** requires `yyyy-MM-dd` + `T` + `HH:mm:ss[.fff]` + `Z`.
- **`NanoidRule`** requires exactly 21 URL-safe characters; **`Cuid2Rule`** requires non-empty lowercase alphanumerics.
- **`StartsWithRule`**, **`EndsWithRule`**, and **`IncludesRule`** use ordinal comparison.

## Number rules

Fluent methods live on `ZodNumber` (`ZodSharp.Schemas`), which validates `double`.

| Rule | Constructor | Fluent method | Code | Message format |
|---|---|---|---|---|
| `MinValueRule<T>` | `(T minValue)` | `Min(v)`, `NonNegative()` | `too_small` | `Value must be at least {0}, but got {1}` |
| `MaxValueRule<T>` | `(T maxValue)` | `Max(v)`, `NonPositive()` | `too_big` | `Value must be at most {0}, but got {1}` |
| `GreaterThanRule<T>` | `(T exclusiveMinimum)` | `Gt(v)`, `Positive()` | `too_small` | `Value must be greater than {0}, but got {1}` |
| `LessThanRule<T>` | `(T exclusiveMaximum)` | `Lt(v)`, `Negative()` | `too_big` | `Value must be less than {0}, but got {1}` |
| `GreaterThanOrEqualRule` | `(double minValue, string? message)` | `Gte(v)` | `too_small` | `Value must be greater than or equal to {0}, but got {1}` |
| `LessThanOrEqualRule` | `(double maxValue, string? message)` | `Lte(v)` | `too_big` | `Value must be less than or equal to {0}, but got {1}` |
| `EvenRule<T>` | `(string? message)` | `Even()` | `invalid_value` | `Number must be even, but got {0}` |
| `OddRule<T>` | `(string? message)` | `Odd()` | `invalid_value` | `Number must be odd, but got {0}` |
| `MultipleOfRule` | `(double divisor, string? message)` | `MultipleOf(divisor)` | `not_multiple_of` | `Number must be a multiple of {0}, but got {1}` |
| `FiniteRule` | `(string? message)` | `Finite()` | `not_finite` | `Number must be finite, but got {0}` |
| `SafeIntegerRule` | `(string? message)` | `Safe()` | `too_big` | `Number must be a safe integer, but got {0}` |
| `IntRule` | `()` | `Int()` | `invalid_type` | `Expected integer, but got {0}` |

Behaviour notes:

- The bound rules (`MinValueRule<T>`, `MaxValueRule<T>`, `GreaterThanRule<T>`, `LessThanRule<T>`) are generic over `T : IComparable<T>`, so they can be reused with any comparable type (the fluent methods close them with `double`). `ZodNumber`'s `Gte`/`Lte` use the non-generic `GreaterThanOrEqualRule`/`LessThanOrEqualRule`; `ZodBigInt` and `ZodDate` close the generic rules with `long` and `DateTime`.
- `Positive()` is `GreaterThanRule<double>(0.0)`, `Negative()` is `LessThanRule<double>(0.0)`, `NonNegative()` is `MinValueRule<double>(0.0)`, and `NonPositive()` is `MaxValueRule<double>(0.0)`.
- **`EvenRule<T>`** and **`OddRule<T>`** are generic over `T : INumber<T>`, so they close with `int`, `long`, `double`, `decimal`, and every other numeric type; `Even()`/`Odd()` close them with `double`. A value is even when `value % 2 == 0`.
- **`MultipleOfRule`** throws `ArgumentException` when the divisor is `0`, and compares the quotient to its nearest integer with a relative tolerance of `1e-12` (so `0.3` is accepted for a divisor of `0.1`).
- **`FiniteRule`** rejects `NaN` and infinities via `double.IsFinite`.
- **`SafeIntegerRule`** requires a whole number within `int.MinValue`..`int.MaxValue`.
- **`IntRule`** requires `value == Math.Truncate(value)`.

## Value rules

| Rule | Constructor | Fluent method | Code | Message format |
|---|---|---|---|---|
| `NonSentinelRule<T>` | `(string? message)` | `NonSentinel()` | `invalid_value` | `Value is a sentinel value, but got {0}` |

`NonSentinelRule<T>` rejects the framework default/boundary values an ORM commonly stores to mean "no value": `Guid.Empty`; `DateTime`, `DateTimeOffset`, `DateOnly`, and `TimeOnly` `MinValue`/`MaxValue`; and `null`/empty/whitespace strings. Types without a known sentinel always pass, so the rule never rejects a type it does not understand. It detects sentinels with a `typeof(T)` dispatch and reinterprets the value in place, so no boxing occurs.

The fluent `NonSentinel()` method is declared on `ZodType<TOutput, TInput>`, so it is available on every schema and closes the rule with the schema's output type. `ZodString` and `ZodDate` override it with a covariant return type so the fluent chain keeps the concrete schema:

```csharp
var schema = Z.Date().NonSentinel();
var result = schema.Validate(DateTime.MinValue);
// result.Errors[0].Code == NonSentinelRule<DateTime>.ErrorCode   ("invalid_value")

// Covariant overrides keep the concrete schema, so later fluent calls still compile.
var chained = Z.String().NonSentinel().Min(3);
```

The same rule can be attached directly with `AddRule`/`Rule`, or through the shipped `[NonSentinel]` attribute, which closes the open generic with the annotated member/scalar type — see [Custom Rules](Custom-Rules.md#non-sentinel-values-ef-friendly).

## Generated attributes

Each built-in rule that can be expressed as an attribute ships a generated `ValidationAttribute` in the `ZodSharp.Rules` namespace (inside the `Purview.ZodSharp` assembly), so a member or a scalar can be annotated directly:

| Attribute | Rule | Notes |
|---|---|---|
| `[Email]` | `EmailRule` | |
| `[E164]` | `E164Rule` | |
| `[Regex(Pattern = "…")]` | `RegexRule` | mirrors the `(string pattern, string? message)` overload |
| `[UUID]` / `[UUID(Version = …)]` | `UUIDRule` | |
| `[ULID]` | `ULIDRule` | |
| `[JWT]` | `JWTRule` | |
| `[IPAddress]` | `IPAddressRule` | |
| `[Hex]` | `HexRule` | |
| `[Base64Url]` | `Base64UrlRule` | |
| `[Nanoid]` | `NanoidRule` | |
| `[Cuid2]` | `Cuid2Rule` | |
| `[DateString]` | `DateStringRule` | |
| `[TimeString]` | `TimeStringRule` | |
| `[DatetimeString]` | `DatetimeStringRule` | |
| `[StartsWith(Prefix = "…")]` | `StartsWithRule` | |
| `[EndsWith(Suffix = "…")]` | `EndsWithRule` | |
| `[Includes(Substring = "…")]` | `IncludesRule` | |
| `[MultipleOf(Divisor = …)]` | `MultipleOfRule` | |
| `[Finite]` | `FiniteRule` | |
| `[SafeInteger]` | `SafeIntegerRule` | |
| `[Int]` | `IntRule` | |
| `[Uri(UriKind = …)]` | `UriRule` | |
| `[NonSentinel]` | `NonSentinelRule<T>` | closes the open generic with the member/scalar type |
| `[MinValue(MinValue = …)]` | `MinValueRule<T>` | the type-parameter bound is a `double`; the rule closes with the member type |
| `[MaxValue(MaxValue = …)]` | `MaxValueRule<T>` | as above |
| `[GreaterThan(ExclusiveMinimum = …)]` | `GreaterThanRule<T>` | as above |
| `[LessThan(ExclusiveMaximum = …)]` | `LessThanRule<T>` | as above |
| `[GreaterThanOrEqual(MinValue = …)]` | `GreaterThanOrEqualRule` | |
| `[LessThanOrEqual(MaxValue = …)]` | `LessThanOrEqualRule` | |
| `[Even]` | `EvenRule<T>` | closes the open generic with the member type |
| `[Odd]` | `OddRule<T>` | closes the open generic with the member type |
| `[MinLengthZod(MinLength = …)]` | `MinLengthRule` | suffixed to avoid the DataAnnotations name; exposes `Code`/`Message` |
| `[MaxLengthZod(MaxLength = …)]` | `MaxLengthRule` | as above |
| `[UrlZod]` | `UrlRule` | as above |
| `[PhoneZod]` | `PhoneRule` | as above |
| `[CreditCardZod]` | `CreditCardRule` | as above |
| `[Base64StringZod]` | `Base64StringRule` | as above |

Each generated attribute exposes a `Message` property when its rule has a `message` constructor parameter, and reports the rule's own `ErrorCode`/`Origin` because every built-in rule implements `IZodRule`.

Rules whose derived attribute name is already taken by `System.ComponentModel.DataAnnotations` — `MinLengthRule`, `MaxLengthRule`, `UrlRule`, `PhoneRule`, `CreditCardRule`, `Base64StringRule` — are emitted under a `Zod` suffix instead (for example `[MinLengthZod]`), so the rule's own identity and message stay usable. The generic bound rules surface their type-parameter bound as a `double`, so `[MinValue(MinValue = 3)]` works on an `int` or a `double` member.

## Exposed as DataAnnotations attributes

The `[ZodSchema]` generator maps several built-in rules to their `System.ComponentModel.DataAnnotations` attributes, and any custom rule can be mapped with `[ZodRule(typeof(...))]`:

| Attribute | Rule | Failure code |
|---|---|---|
| `[EmailAddress]` | `EmailRule` | `invalid_string` |
| `[Url]` | `UrlRule` | `invalid_string` |
| `[Phone]` | `PhoneRule` | `invalid_string` |
| `[CreditCard]` | `CreditCardRule` | `invalid_string` |
| `[Base64String]` | `Base64StringRule` | `invalid_string` |
| `[RegularExpression]` | compiled `Regex`, not `RegexRule` | `invalid_string` |

Size and range attributes (`[Length]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`, `[Range]`) are emitted as direct, typed codegen rather than as rule structs. The rule-identity variants of the colliding names are the suffixed [generated attributes](#generated-attributes) (`[MinLengthZod]`, `[MaxLengthZod]`, `[UrlZod]`, `[PhoneZod]`, `[CreditCardZod]`, `[Base64StringZod]`), which report the rule's own `Code` and accept a `Message`. The remaining built-in rules are covered by their own generated attributes — for example `[Regex(Pattern = "…")]` validates through `RegexRule`, whereas `[RegularExpression]` compiles the pattern inline. See [Source Generator DataAnnotations](Source-Generator-DataAnnotations.md) for the full attribute table and the generated metadata, and [Custom Rules](Custom-Rules.md) for mapping your own rules to attributes.

## Related

- [Custom Rules](Custom-Rules.md) — the `IValidationRule<T>` contract, `IZodRule`, and attribute mapping.
- [String Validation](String-Validation.md) — the `ZodString` fluent surface and span validation.
- [Number Validation](Number-Validation.md) — the `ZodNumber` fluent surface.
- [Source Generator DataAnnotations](Source-Generator-DataAnnotations.md) — attributes the generator understands.
- [Source Generator Diagnostics](Source-Generator-Diagnostics.md) — `ZODSGEN042` and the rule conventions analyzer.
