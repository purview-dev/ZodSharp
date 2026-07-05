using System.Collections.Immutable;

namespace ZodSharp.Core;

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ZodException : Exception
{
	/// <summary>
	/// The validation errors that caused this exception.
	/// </summary>
	public ImmutableArray<ValidationError> Errors { get; }

	/// <summary>
	/// Initializes a new instance of the ZodException class.
	/// </summary>
	/// <param name="errors">The validation errors</param>
	public ZodException(ImmutableArray<ValidationError> errors)
		: base($"Validation failed with {errors.Length} error(s)")
	{
		Errors = errors;
	}

	/// <summary>
	/// Initializes a new instance of the ZodException class.
	/// </summary>
	/// <param name="errors">The validation errors</param>
	public ZodException(IEnumerable<ValidationError> errors)
		: this([.. errors]) { }

	/// <summary>
	/// Returns a string representation of the exception including all validation errors.
	/// </summary>
	/// <returns>A string representation of the exception</returns>
	public override string ToString()
	{
		if (Errors.IsDefaultOrEmpty)
			return base.ToString();

		var errorMessages = Errors.Select(e => $"{string.Join(".", e.Path)}: {e.Message} ({e.Code})");

		return $"{Message}\n{string.Join("\n", errorMessages)}";
	}

	/// <inheritdoc/>
	public ZodException() { }

	/// <inheritdoc/>
	public ZodException(string message)
		: base(message) { }

	/// <inheritdoc/>
	public ZodException(string message, Exception innerException)
		: base(message, innerException) { }
}
