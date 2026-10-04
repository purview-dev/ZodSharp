using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ZodSharp.Core;
using ZodSharp.Rules;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for string validation.
/// Provides fluent API for common string validations.
/// </summary>
public class ZodString : ZodType<string>
{
	static readonly string[] EmptyPath = [];

	// Rules that also implement IStringValidationRule, so ValidateSpan/IsValidSpan can validate the
	// incoming span without materialising a string. When any rule lacks the span contract the list is
	// abandoned and span validation falls back to the string path.
	ImmutableArray<IStringValidationRule> _spanRules = [];
	bool _spanRulesSupported = true;

	/// <summary>
	/// Gets whether span validation can bypass materialising the input string.
	/// </summary>
	protected virtual bool SupportsSpanValidation => true;

	/// <inheritdoc/>
	public override ZodType<string, string> AddRule(IValidationRule<string> rule)
	{
		base.AddRule(rule);

		if (_spanRulesSupported)
		{
			if (rule is IStringValidationRule spanRule)
				_spanRules = _spanRules.Add(spanRule);
			else
				_spanRulesSupported = false;
		}

		return this;
	}

	/// <summary>
	/// Parses and validates a string value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<string> ParseInternal(string value) =>
		value == null
			? ValidationResult<string>.Failure(
				new ValidationError("invalid_type", "Expected string, but got null", EmptyPath)
			)
			: ValidationResult<string>.Success(value);

	/// <summary>
	/// Validates a <see cref="ReadOnlySpan{T}"/> of characters using the accumulated rules' span path.
	/// </summary>
	/// <param name="value">The span to validate.</param>
	/// <returns>A validation result. The returned value is a string, so a successful validation still allocates once.</returns>
	/// <remarks>
	/// When every accumulated rule implements <see cref="IStringValidationRule"/> the input is never
	/// materialised; otherwise the span is converted to a string and validated through
	/// <c>Validate</c>. Use <see cref="IsValidSpan"/> when no validated value is
	/// needed: it does not allocate on success.
	/// </remarks>
	public ValidationResult<string> ValidateSpan(ReadOnlySpan<char> value)
	{
		// Transforms (and any rule without the span contract) must run through the string pipeline so the
		// produced value is preserved.
		if (!SupportsSpanRules)
			return Validate(value.ToString());

		// Every rule supports the span contract, so validate the span directly and return a string result.
		return IsValidSpan(value, out var errors)
			? ValidationResult<string>.Success(value.ToString())
			: ValidationResult<string>.Failure(errors);
	}

	/// <summary>
	/// Validates a span without materialising the input string.
	/// </summary>
	/// <param name="value">The span to validate.</param>
	/// <param name="errors">The validation errors when the span is invalid; empty when it is valid.</param>
	/// <returns><see langword="true"/> when the span is valid.</returns>
	/// <remarks>
	/// This is the allocation-free span entry point: it validates the span directly when every rule
	/// implements <see cref="IStringValidationRule"/>, and otherwise falls back to the string path once.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public bool IsValidSpan(ReadOnlySpan<char> value, out ImmutableArray<ValidationError> errors)
	{
		errors = [];

		if (!SupportsSpanRules)
		{
			var result = Validate(value.ToString());
			errors = result.Errors;
			return result.IsSuccess;
		}

		ImmutableArray<ValidationError>.Builder? builder = null;
		foreach (var rule in _spanRules)
		{
			if (rule.IsValid(value))
				continue;

			builder ??= ImmutableArray.CreateBuilder<ValidationError>();
			builder.Add(new ValidationError(rule.Code, rule.GetErrorMessage(value), EmptyPath));
		}

		if (builder is null)
			return true;

		errors = builder.ToImmutable();
		return false;
	}

	bool SupportsSpanRules => _spanRulesSupported && SupportsSpanValidation && _spanRules.Length == RuleCount;

	/// <summary>
	/// Adds a minimum length validation.
	/// </summary>
	/// <param name="minLength">The minimum length</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Min(int minLength)
	{
		AddRule(new MinLengthRule(minLength));
		return this;
	}

	/// <summary>
	/// Adds a maximum length validation.
	/// </summary>
	/// <param name="maxLength">The maximum length</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Max(int maxLength)
	{
		AddRule(new MaxLengthRule(maxLength));
		return this;
	}

	/// <summary>
	/// Adds an email format validation.
	/// </summary>
	/// <returns>This schema for method chaining</returns>
	public ZodString Email()
	{
		AddRule(new EmailRule());
		return this;
	}

	/// <summary>
	/// Adds a regex pattern validation.
	/// </summary>
	/// <param name="pattern">The regex pattern</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Regex(Regex pattern, string? message = null)
	{
		AddRule(new RegexRule(pattern, message));
		return this;
	}

	/// <summary>
	/// Adds a regex pattern validation from a string.
	/// </summary>
	/// <param name="pattern">The regex pattern string</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Regex(string pattern, string? message = null)
	{
		Regex regex = new(pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
		return Regex(regex, message);
	}

	/// <summary>
	/// Sets the exact string length.
	/// </summary>
	/// <param name="length">The exact length</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Length(int length)
	{
		AddRule(new MinLengthRule(length));
		AddRule(new MaxLengthRule(length));
		return this;
	}

	/// <summary>
	/// Adds a URL format validation.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Naming",
		"PDS0004:Use correct acronym capitalization",
		Justification = "Name is real"
	)]
	public ZodString Url(string? message = null)
	{
		AddRule(new UrlRule(message));
		return this;
	}

	/// <summary>
	/// Adds a URI format validation that accepts a relative or absolute URI.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Uri(string? message = null) => Uri(UriKind.RelativeOrAbsolute, message);

	/// <summary>
	/// Adds a URI format validation requiring the specified <see cref="UriKind"/>.
	/// </summary>
	/// <param name="uriKind">The kind of URI the value must be</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Uri(UriKind uriKind, string? message = null)
	{
		AddRule(new UriRule(uriKind, message));
		return this;
	}

	/// <summary>
	/// Adds a phone number format validation.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Phone(string? message = null)
	{
		AddRule(new PhoneRule(message));
		return this;
	}

	/// <summary>
	/// Adds a credit card number format validation.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString CreditCard(string? message = null)
	{
		AddRule(new CreditCardRule(message));
		return this;
	}

	/// <summary>
	/// Adds a Base64 string format validation.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Base64String(string? message = null)
	{
		AddRule(new Base64StringRule(message));
		return this;
	}

	/// <summary>
	/// Adds a UUID format validation. Accepts RFC 9562 versions 1-8 with the RFC
	/// variant nibble, plus the nil and max UUIDs.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString UUID(string? message = null)
	{
		AddRule(new UUIDRule(message));
		return this;
	}

	/// <summary>
	/// Adds a UUID format validation requiring a specific RFC 9562 version.
	/// </summary>
	/// <param name="version">The required UUID version</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString UUID(UuidVersion version, string? message = null)
	{
		AddRule(new UUIDRule(version, message));
		return this;
	}

	/// <summary>
	/// Adds a validation that the string must start with the specified prefix.
	/// </summary>
	/// <param name="prefix">The required prefix</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString StartsWith(string prefix, string? message = null)
	{
		AddRule(new StartsWithRule(prefix, message));
		return this;
	}

	/// <summary>
	/// Adds a validation that the string must end with the specified suffix.
	/// </summary>
	/// <param name="suffix">The required suffix</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString EndsWith(string suffix, string? message = null)
	{
		AddRule(new EndsWithRule(suffix, message));
		return this;
	}

	/// <summary>
	/// Adds a validation that the string contains the specified substring.
	/// Equivalent to Zod's <c>z.string().includes(value)</c>.
	/// </summary>
	/// <param name="substring">The required substring</param>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Includes(string substring, string? message = null)
	{
		AddRule(new IncludesRule(substring, message));
		return this;
	}

	/// <summary>
	/// Adds an IPv4 or IPv6 address format validation.
	/// Equivalent to Zod's <c>z.string().ip()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString IP(string? message = null)
	{
		AddRule(new IPAddressRule(message));
		return this;
	}

	/// <summary>
	/// Adds a JSON Web Token (JWT) format validation: three base64url-encoded segments separated by periods.
	/// Equivalent to Zod's <c>z.string().jwt()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString JWT(string? message = null)
	{
		AddRule(new JWTRule(message));
		return this;
	}

	/// <summary>
	/// Adds a hexadecimal string format validation.
	/// Equivalent to Zod's <c>z.string().hex()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Hex(string? message = null)
	{
		AddRule(new HexRule(message));
		return this;
	}

	/// <summary>
	/// Adds a base64url (URL-safe base64) string format validation.
	/// Equivalent to Zod's <c>z.string().base64url()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Naming",
		"PDS0004:Use correct acronym capitalization",
		Justification = "Name matches Zod's base64url() method."
	)]
	public ZodString Base64Url(string? message = null)
	{
		AddRule(new Base64UrlRule(message));
		return this;
	}

	/// <summary>
	/// Adds a ULID format validation (26 Crockford base32 characters).
	/// Equivalent to Zod's <c>z.string().ulid()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString ULID(string? message = null)
	{
		AddRule(new ULIDRule(message));
		return this;
	}

	/// <summary>
	/// Adds an ISO 8601 date-time format validation.
	/// Equivalent to Zod's <c>z.string().datetime()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Datetime(string? message = null)
	{
		AddRule(new DatetimeStringRule(message));
		return this;
	}

	/// <summary>
	/// Adds an ISO 8601 date (yyyy-MM-dd) format validation.
	/// Equivalent to Zod's <c>z.string().date()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Date(string? message = null)
	{
		AddRule(new DateStringRule(message));
		return this;
	}

	/// <summary>
	/// Adds an ISO 8601 time format validation.
	/// Equivalent to Zod's <c>z.string().time()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Time(string? message = null)
	{
		AddRule(new TimeStringRule(message));
		return this;
	}

	/// <summary>
	/// Adds a nanoid format validation (21 URL-safe characters).
	/// Equivalent to Zod's <c>z.string().nanoid()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Nanoid(string? message = null)
	{
		AddRule(new NanoidRule(message));
		return this;
	}

	/// <summary>
	/// Adds a CUID2 format validation (lowercase alphanumeric characters).
	/// Equivalent to Zod's <c>z.string().cuid2()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Cuid2(string? message = null)
	{
		AddRule(new Cuid2Rule(message));
		return this;
	}

	/// <summary>
	/// Adds an E.164 phone number format validation (+ followed by 7-15 digits).
	/// Equivalent to Zod's <c>z.string().e164()</c>.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString E164(string? message = null)
	{
		AddRule(new E164Rule(message));
		return this;
	}

	/// <summary>
	/// Adds a non-sentinel validation that rejects <c>null</c>, empty, and whitespace strings.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public override ZodString NonSentinel(string? message = null)
	{
		AddRule(new NonSentinelRule<string>(message));
		return this;
	}

	/// <summary>
	/// Transforms the string to lowercase.
	/// </summary>
	/// <returns>A new schema that transforms the value</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
	public ZodString ToLower()
	{
		var transform = Transform(static s => s.ToLowerInvariant());
		return new ZodStringWrapper(transform);
	}

	/// <summary>
	/// Transforms the string to uppercase.
	/// </summary>
	/// <returns>A new schema that transforms the value</returns>
	public ZodString ToUpper()
	{
		var transform = Transform(static s => s.ToUpperInvariant());
		return new ZodStringWrapper(transform);
	}

	/// <summary>
	/// Trims whitespace from the string.
	/// </summary>
	/// <returns>A new schema that transforms the value</returns>
	public ZodString Trim()
	{
		var transform = Transform(static s => s.Trim());
		return new ZodStringWrapper(transform);
	}

	class ZodStringWrapper(ZodTransform<string, string> transform) : ZodString
	{
		protected override bool SupportsSpanValidation => false;

		protected override ValidationResult<string> ParseInternal(string value) => transform.Validate(value);
	}
}
