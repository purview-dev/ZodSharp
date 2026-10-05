namespace ZodSharp.Core;

/// <summary>
/// Implemented by validation rules that describe their own error identity. When a mapped rule implements
/// this interface the generator prefers the rule's <see cref="Code"/>/<see cref="Origin"/> over the values
/// declared on the mapped attribute, so a single attribute (for example <c>[NotEmpty]</c>) can produce a
/// different error code per annotated member.
/// </summary>
/// <remarks>
/// <para>
/// Rules are constructed in the generated code, so the identity can depend on the rule's constructor
/// arguments. The interface may be implemented explicitly; the generated code casts to
/// <see cref="IZodRule"/> when reading the values.
/// </para>
/// <para>
/// This interface is the only path for a constructor argument to reach the reported identity: a rule that
/// accepts a <c>code</c> or <c>origin</c> parameter without implementing it is reported as <c>ZODSGEN039</c>,
/// because the value would be supplied by the generated validation and then never read. See
/// <see cref="IZodRuleAttribute"/> for the attribute-side counterpart.
/// </para>
/// </remarks>
public interface IZodRule
{
	/// <summary>
	/// Gets the effective error code for the constructed rule, or <see langword="null"/> to defer to the
	/// attribute-mapped code (and then to <c>"validation_failed"</c>). A rule that accepts a <c>code</c>
	/// override typically returns it here and from its <see cref="IValidationRule{T}.Code"/> /
	/// <see cref="IStringValidationRule.Code"/> implementation, so every route reports the same value.
	/// </summary>
	string? Code { get; }

	/// <summary>
	/// Gets the structured <see cref="ValidationError.Origin"/> reported when the rule fails, or
	/// <see langword="null"/> to fall back to the attribute-mapped origin.
	/// </summary>
	string? Origin { get; }
}
