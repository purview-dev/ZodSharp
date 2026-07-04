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
	static readonly string[] EmptyPath = Array.Empty<string>();

	/// <summary>
	/// Parses and validates a string value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>A validation result</returns>
	protected override ValidationResult<string> ParseInternal(string value)
	{
		if (value == null)
		{
			return ValidationResult<string>.Failure(
				new ValidationError("invalid_type", "Expected string, but got null", EmptyPath)
			);
		}

		return ValidationResult<string>.Success(value);
	}

	/// <summary>
	/// Validates a ReadOnlySpan of characters without allocating a string.
	/// </summary>
	/// <param name="value">The span to validate</param>
	/// <returns>A validation result</returns>
	public ValidationResult<string> ValidateSpan(ReadOnlySpan<char> value)
	{
		if (value.IsEmpty && value.Length == 0)
		{
			return ValidationResult<string>.Success(string.Empty);
		}

		var str = value.ToString();
		return Validate(str);
	}

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
	public ZodString Regex(System.Text.RegularExpressions.Regex pattern, string? message = null)
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
		var regex = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
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
	public ZodString Url(string? message = null)
	{
		AddRule(new UrlRule(message));
		return this;
	}

	/// <summary>
	/// Adds a UUID format validation.
	/// </summary>
	/// <param name="message">Optional error message</param>
	/// <returns>This schema for method chaining</returns>
	public ZodString Uuid(string? message = null)
	{
		AddRule(new UuidRule(message));
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
	/// Transforms the string to lowercase.
	/// </summary>
	/// <returns>A new schema that transforms the value</returns>
	public ZodString ToLower()
	{
		var transform = Transform(s => s.ToLowerInvariant());
		return new ZodStringWrapper(transform);
	}

	/// <summary>
	/// Transforms the string to uppercase.
	/// </summary>
	/// <returns>A new schema that transforms the value</returns>
	public ZodString ToUpper()
	{
		var transform = Transform(s => s.ToUpperInvariant());
		return new ZodStringWrapper(transform);
	}

	/// <summary>
	/// Trims whitespace from the string.
	/// </summary>
	/// <returns>A new schema that transforms the value</returns>
	public ZodString Trim()
	{
		var transform = Transform(s => s.Trim());
		return new ZodStringWrapper(transform);
	}

	class ZodStringWrapper : ZodString
	{
		readonly ZodTransform<string, string> _transform;

		public ZodStringWrapper(ZodTransform<string, string> transform)
		{
			_transform = transform;
		}

		protected override ValidationResult<string> ParseInternal(string value)
		{
			return _transform.Validate(value);
		}
	}
}
