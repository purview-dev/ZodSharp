namespace ZodSharp.SourceGenerators.Models;

/// <summary>
/// Immutable result of <c>OnZodValidate</c> refinement hook discovery for a <c>[ZodSchema]</c> target.
/// </summary>
/// <param name="IsImplemented">
/// True when the target supplies a hook body, so the generated <c>Validate</c> invokes it. A target that
/// only leaves the generated declaration in place contributes no refinement and allocates nothing.
/// </param>
/// <param name="DeclarationChain">
/// The containing types (outermost first) followed by the target type itself, when every type in the
/// chain is declared <c>partial</c> so the generator can reopen them to declare the hook. Empty when the
/// chain cannot be reopened, in which case no declaration is emitted.
/// </param>
readonly record struct ZodRefinementHookData(
	bool IsImplemented,
	EquatableArray<TypeDeclarationOptions> DeclarationChain
)
{
	/// <summary>No hook is implemented and no hook declaration can be emitted.</summary>
	public static readonly ZodRefinementHookData None = new(false, []);

	/// <summary>True when the generator emits the hook declaration onto the target type.</summary>
	public bool CanEmitDeclaration => DeclarationChain.Count > 0;
}
