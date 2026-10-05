namespace ZodSharp.Core;

/// <summary>
/// Interface for validation rules.
/// Implementations should be structs to avoid allocations.
/// </summary>
/// <typeparam name="T">The type of value being validated</typeparam>
public interface IValidationRule<T>
{
	/// <summary>
	/// Validates the value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	bool IsValid(in T value);

	/// <summary>
	/// Gets the error message if validation fails.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	string GetErrorMessage(in T value);

	/// <summary>
	/// Gets the Zod-compatible error code reported when the rule fails. A rule that accepts a per-usage
	/// override returns it; otherwise the rule's canonical default is returned. Defaults to
	/// <c>"validation_failed"</c> when the rule does not declare a code.
	/// </summary>
	string Code => "validation_failed";
}
