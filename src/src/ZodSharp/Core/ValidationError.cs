using System.Collections.Immutable;

namespace ZodSharp.Core;

/// <summary>
/// Represents a validation error.
/// Uses struct to minimize allocations.
/// </summary>
public readonly record struct ValidationError
{
	/// <summary>
	/// The error code (e.g., "invalid_type", "too_small", "too_big")
	/// </summary>
	public string Code { get; }

	/// <summary>
	/// The error message
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// The path to the field that failed validation (e.g., ["user", "email"])
	/// </summary>
	public ImmutableArray<string> Path { get; }

	/// <summary>
	/// Additional error parameters
	/// </summary>
	public IReadOnlyDictionary<string, object?>? Parameters { get; }

	/// <summary>
	/// Initializes a new instance of the ValidationError struct.
	/// </summary>
	/// <param name="code">The error code</param>
	/// <param name="message">The error message</param>
	/// <param name="path">The path to the field that failed validation</param>
	/// <param name="parameters">Additional error parameters</param>
	public ValidationError(
		string code,
		string message,
		string[]? path = null,
		IReadOnlyDictionary<string, object?>? parameters = null
	)
	{
		Code = code;
		Message = message;
		Path = path is null ? [] : ImmutableArray.Create(path);
		Parameters = parameters;
	}
}
