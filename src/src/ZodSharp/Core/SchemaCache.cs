using System.Collections.Concurrent;

namespace ZodSharp.Core;

/// <summary>
/// Provides intelligent caching of schemas to avoid re-creation.
/// Thread-safe and optimized for high-performance scenarios.
/// </summary>
public static class SchemaCache
{
	static readonly ConcurrentDictionary<string, object> _cache = new();

	/// <summary>
	/// Gets or creates a schema using a cache key.
	/// </summary>
	public static T GetOrCreate<T>(string key, Func<T> factory)
		where T : class
	{
		return (T)_cache.GetOrAdd(key, _ => factory()!);
	}

	/// <summary>
	/// Clears the schema cache.
	/// </summary>
	public static void Clear()
	{
		_cache.Clear();
	}

	/// <summary>
	/// Gets the number of cached schemas.
	/// </summary>
	public static int Count => _cache.Count;

	/// <summary>
	/// Removes a specific schema from the cache.
	/// </summary>
	public static bool Remove(string key)
	{
		return _cache.TryRemove(key, out _);
	}
}
