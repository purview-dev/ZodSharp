using System.Collections;

namespace ZodSharp.Optimizations;

/// <summary>
/// Helpers for counting collections and enumerable sequences without repeated enumeration.
/// </summary>
public static class CollectionCountHelper
{
	/// <summary>
	/// Counts a generic sequence using a non-enumerating fast path where possible.
	/// Falls back to a single enumeration when no count is available.
	/// </summary>
	public static int GetCount<T>(IEnumerable<T> sequence)
	{
		if (sequence is null)
			throw new ArgumentNullException(nameof(sequence));

		if (sequence is ICollection<T> collection)
			return collection.Count;

		if (sequence is IReadOnlyCollection<T> readOnlyCollection)
			return readOnlyCollection.Count;

		if (sequence is ICollection nonGenericCollection)
			return nonGenericCollection.Count;

		var count = 0;
		using var enumerator = sequence.GetEnumerator();
		while (enumerator.MoveNext())
			count++;

		return count;
	}

	/// <summary>
	/// Counts a non-generic sequence using a non-enumerating fast path where possible.
	/// Falls back to a single enumeration when no count is available.
	/// </summary>
	public static int GetCount(IEnumerable sequence)
	{
		if (sequence is null)
			throw new ArgumentNullException(nameof(sequence));

		if (sequence is ICollection collection)
			return collection.Count;

		var count = 0;
		var enumerator = sequence.GetEnumerator();
		while (enumerator.MoveNext())
			count++;

		return count;
	}
}
