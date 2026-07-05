using System.Collections.Concurrent;

namespace ZodSharp.Core;

/// <summary>
/// Provides intelligent caching of schemas to avoid re-creation.
/// Thread-safe and optimized for high-performance scenarios.
/// </summary>
public static class SchemaCache
{
	static readonly ConcurrentDictionary<string, object> Cache = new();

	/// <summary>
	/// Gets or creates a schema using a cache key.
	/// </summary>
	public static T GetOrCreate<T>(string key, Func<T> factory)
		where T : class => (T)Cache.GetOrAdd(key, _ => factory()!);

	/// <summary>
	/// Clears the schema cache.
	/// </summary>
	public static void Clear() => Cache.Clear();

	/// <summary>
	/// Gets the number of cached schemas.
	/// </summary>
	public static int Count => Cache.Count;

	/// <summary>
	/// Removes a specific schema from the cache.
	/// </summary>
	public static bool Remove(string key) => Cache.TryRemove(key, out _);
}
