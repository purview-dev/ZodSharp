#if NET11_0_OR_GREATER
#pragma warning disable CA1815 // Native unions do not synthesize equality; the schema result does not need it.
namespace ZodSharp.Unions;

/// <summary>
/// A native C# 15 union of two types, produced by <c>Z.NativeUnion&lt;T1, T2&gt;</c>.
/// </summary>
/// <typeparam name="T1">The first case type.</typeparam>
/// <typeparam name="T2">The second case type.</typeparam>
/// <remarks>
/// <para>
/// Only available when targeting <c>net11.0</c> or later, where the C# 15 <c>union</c> keyword and
/// <see cref="System.Runtime.CompilerServices.IUnion"/> exist. The compiler lowers this to a struct
/// with a single <see cref="object"/> backing field: reference-type cases are allocation-free, while
/// value-type cases box. Prefer the hand-rolled <see cref="Union{T1,T2}"/> when a case is a value
/// type, or when <c>Tag</c>/<c>Match</c>/<c>Switch</c>/equality are required.
/// </para>
/// </remarks>
public readonly union NativeUnion<T1, T2>(T1, T2);
#pragma warning restore CA1815
#endif
