namespace ZodSharp.Core;

/// <summary>
/// Identifies validation rules that reject an absent (<see langword="null"/>) value, such as
/// <see cref="Rules.RequiredRule{T}"/>.
/// </summary>
/// <remarks>
/// A rule bound to a nullable member is normally emitted inside the non-null guard the schema generator
/// applies, so a missing value never reaches it. A rule that implements this contract is instead emitted
/// outside that guard, so an absent value is reported exactly as
/// <c>System.ComponentModel.DataAnnotations.RequiredAttribute</c> reports it.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1040:Avoid empty interfaces",
	Justification = "Marker interface consumed by the schema generator."
)]
public interface IRequiredRule { }
