using System.Collections.Immutable;
using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Context passed to a <see cref="ZodType{TOutput,TInput}.SuperRefine"/> callback.
/// Mirrors Zod's <c>ctx</c> argument to <c>superRefine</c>, allowing refinements
/// to emit multiple, path-located issues rather than a single boolean verdict.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="RefineCtx{T}"/> class with the
/// value under validation and an optional base path.
/// </remarks>
/// <param name="value">The validated value.</param>
/// <param name="path">The base path prepended to any added issues.</param>
public sealed class RefineCtx<T>(T value, ImmutableArray<string> path)
{
	readonly List<ValidationError> _issues = [];

	/// <summary>
	/// The value being validated.
	/// </summary>
	public T Value { get; } = value;

	/// <summary>
	/// The base path under which issues are reported.
	/// </summary>
	public ImmutableArray<string> Path { get; } = path;

	/// <summary>
	/// The issues added by the refinement so far.
	/// </summary>
	public IReadOnlyList<ValidationError> Issues => _issues;

	/// <summary>
	/// True when at least one issue has been added.
	/// </summary>
	public bool HasIssues => _issues.Count > 0;

	/// <summary>
	/// Adds a validation issue with the given code and message, prefixed by the
	/// context's base path.
	/// </summary>
	/// <param name="code">The issue code (e.g. <c>refinement_failed</c>).</param>
	/// <param name="message">The human-readable issue message.</param>
	/// <param name="path">An optional relative path appended to <see cref="Path"/>.</param>
	public void AddIssue(string code, string message, string[]? path = null)
	{
		if (string.IsNullOrWhiteSpace(code))
			throw new ArgumentException("Issue code must not be null or whitespace.", nameof(code));
		if (string.IsNullOrWhiteSpace(message))
			throw new ArgumentException("Issue message must not be null or whitespace.", nameof(message));

		var fullPath = BuildPath(Path, path);
		_issues.Add(new ValidationError(code, message, fullPath));
	}

	/// <summary>
	/// Adds a generic <c>refinement_failed</c> issue with the given message.
	/// Equivalent to calling <see cref="AddIssue(string, string, string[])"/> with code <c>refinement_failed</c>.
	/// </summary>
	/// <param name="message">The human-readable issue message.</param>
	/// <param name="path">An optional relative path appended to <see cref="Path"/>.</param>
	public void AddIssue(string message, string[]? path = null) => AddIssue("refinement_failed", message, path);

	static string[] BuildPath(ImmutableArray<string> basePath, string[]? relativePath)
	{
		if (basePath.IsDefaultOrEmpty && (relativePath is null || relativePath.Length == 0))
			return [];

		if (basePath.IsDefaultOrEmpty)
			return relativePath!;

		if (relativePath is null || relativePath.Length == 0)
		{
			var copy = new string[basePath.Length];
			basePath.CopyTo(copy, 0);
			return copy;
		}

		var combined = new string[basePath.Length + relativePath.Length];
		basePath.CopyTo(combined, 0);
		relativePath.CopyTo(combined, basePath.Length);
		return combined;
	}

	internal ImmutableArray<ValidationError> ToImmutable() => [.. _issues];
}
