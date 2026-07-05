using System.Buffers;

namespace ZodSharp.Optimizations;

/// <summary>
/// Helper methods for zero-allocation operations.
/// Based on techniques for minimizing GC pressure.
/// </summary>
static class ZeroAllocationHelpers
{
	/// <summary>
	/// Rents an array from the pool and returns it when done.
	/// </summary>
	public static T[] RentArray<T>(int minimumLength) => ArrayPool<T>.Shared.Rent(minimumLength);

	/// <summary>
	/// Returns an array to the pool.
	/// </summary>
	public static void ReturnArray<T>(T[] array)
	{
		if (array != null)
		{
			ArrayPool<T>.Shared.Return(array);
		}
	}

	/// <summary>
	/// Counts items matching a condition without LINQ allocations.
	/// </summary>
	public static int CountWhere<T>(IReadOnlyList<T> items, Func<T, bool> predicate)
	{
		int count = 0;
		for (int i = 0; i < items.Count; i++)
		{
			if (predicate(items[i]))
				count++;
		}

		return count;
	}

	/// <summary>
	/// Collects non-null items into a list without LINQ.
	/// </summary>
	public static void CollectNonNull<T>(IEnumerable<T> items, List<T> output)
		where T : class
	{
		output.Clear();
		foreach (var item in items)
		{
			if (item != null)
				output.Add(item);
		}
	}
}
