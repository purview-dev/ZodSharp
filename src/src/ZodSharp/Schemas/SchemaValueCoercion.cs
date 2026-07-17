using System.Globalization;

namespace ZodSharp.Schemas;

/// <summary>
/// Shared coercion logic used by <c>SchemaWrapper&lt;T&gt;</c> in the object and
/// discriminated-union builders, where untyped <see cref="object"/> values from a
/// dictionary (e.g. deserialized JSON) are handed to typed schemas.
/// </summary>
static class SchemaValueCoercion
{
	static readonly Type DoubleType = typeof(double);

	/// <summary>
	/// Attempts to coerce <paramref name="value"/> to <typeparamref name="T"/>.
	/// Returns <see langword="true"/> and sets <paramref name="result"/> on success;
	/// returns <see langword="false"/> when no safe coercion exists (caller should
	/// emit an <c>invalid_type</c> error).
	/// </summary>
	/// <remarks>
	/// Direct assignment is tried first. Numeric coercion (e.g. boxed <see cref="long"/>
	/// to <see cref="double"/>) is applied only when both the source and target are
	/// numeric <see cref="IConvertible"/> types, so non-numeric mismatches (e.g.
	/// <c>"not-a-number"</c> into <see cref="double"/>) still fail as before.
	/// </remarks>
	public static bool TryCoerce<T>(object? value, out T result)
	{
		if (value is T typedValue)
		{
			result = typedValue;
			return true;
		}

		if (value is IConvertible convertible && IsNumericType(typeof(T)))
		{
			var sourceType = value.GetType();
			if (IsNumericType(sourceType))
			{
				try
				{
					result = (T)convertible.ToType(typeof(T), CultureInfo.InvariantCulture);
					return true;
				}
				catch (InvalidCastException)
				{
					// Overflow / unsupported numeric conversion - fall through to failure.
				}
				catch (OverflowException)
				{
					// Value out of target range - fall through to failure.
				}
			}
		}

		result = default!;
		return false;
	}

	static bool IsNumericType(Type type) =>
		type == DoubleType
		|| type == typeof(float)
		|| type == typeof(decimal)
		|| type == typeof(int)
		|| type == typeof(long)
		|| type == typeof(short)
		|| type == typeof(byte)
		|| type == typeof(uint)
		|| type == typeof(ulong)
		|| type == typeof(ushort)
		|| type == typeof(sbyte);
}
