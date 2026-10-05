namespace ZodSharp.Core;

/// <summary>
/// Interface for string validation rules that can work with ReadOnlySpan&lt;char&gt;.
/// This allows zero-allocation validation when working with string spans.
/// </summary>
public interface IStringValidationRule
{
	/// <summary>
	/// Validates the value using a span.
	/// </summary>
	/// <param name="value">The value to validate as a span</param>
	/// <returns>True if valid, false otherwise</returns>
	bool IsValid(ReadOnlySpan<char> value);

	/// <summary>
	/// Gets the error message if validation fails.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	string GetErrorMessage(ReadOnlySpan<char> value);

	/// <summary>
	/// Gets the Zod-compatible error code reported when the rule fails. A rule that accepts a per-usage
	/// override returns it; otherwise the rule's canonical default is returned. Defaults to
	/// <c>"validation_failed"</c> when the rule does not declare a code.
	/// </summary>
	string Code => "validation_failed";
}
