namespace ZodSharp.Unions;

/// <summary>
/// Marker interface for discriminated union value types.
/// </summary>
/// <remarks>
/// <para>
/// ZodSharp's <see cref="Union{T1,T2}"/> (and higher-arity overloads) implement this
/// interface to provide a common, non-boxing discriminated-union surface that mirrors
/// the .NET 11 <c>union</c> keyword's <c>IUnion</c> contract.
/// </para>
/// <para>
/// On .NET 11 and later, the BCL defines <c>System.Runtime.CompilerServices.IUnion</c>;
/// this local marker is forward-compatible — consumer code written against
/// <see cref="IUnion"/> today will work identically when a <c>net11.0</c> target is added.
/// </para>
/// </remarks>
public interface IUnion
{
	/// <summary>
	/// Gets the boxed value of the union. Prefer the non-boxing
	/// <c>TryGetValue</c> overloads in performance-critical paths.
	/// </summary>
	object Value { get; }

	/// <summary>
	/// Gets the zero-based tag identifying which case of the union is active.
	/// </summary>
	int Tag { get; }
}
