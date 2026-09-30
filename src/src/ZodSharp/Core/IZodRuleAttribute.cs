using System.Diagnostics.CodeAnalysis;

namespace ZodSharp.Core;

/// <summary>
/// Declares that a DataAnnotations-style validation attribute participates in the rule error identity, so the
/// <c>[ZodSchema]</c> generator reads its <see cref="Code"/> and <see cref="Origin"/> from the applied attribute.
/// </summary>
/// <remarks>
/// <para>
/// A rule attribute mapped with <c>[ZodRule(typeof(...))]</c> supplies the error identity through named
/// arguments: <c>Code</c> is the reported error code and <c>Origin</c> the structured origin. The generator
/// reads those arguments by name, so a hand-authored attribute that exposes them works without this interface
/// (see <c>ZodSharp.Core.ZodRuleAttribute</c>).
/// </para>
/// <para>
/// Implementing this interface makes the contract explicit and compiler-enforced: the members below must be
/// present, so the generated validation is guaranteed to find them, and it lets the generator treat any future
/// member added here as an identity property rather than an unconsumed attribute argument.
/// </para>
/// </remarks>
[SuppressMessage(
	"Naming",
	"CA1711:Identifiers should not have incorrect suffix",
	Justification = "This interface is the contract that validation attributes implement, so naming it an attribute is intentional."
)]
public interface IZodRuleAttribute
{
	/// <summary>
	/// Gets the error code reported when the rule fails, or <see langword="null"/> to fall back to the code
	/// declared by <c>[ZodRule(Code = "...")]</c> and then to <c>"validation_failed"</c>.
	/// </summary>
	string? Code { get; }

	/// <summary>
	/// Gets the structured <c>ValidationError.Origin</c> reported when the rule fails, or
	/// <see langword="null"/> to fall back to the origin declared by <c>[ZodRule(Origin = "...")]</c>.
	/// </summary>
	string? Origin { get; }
}
