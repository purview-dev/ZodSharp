using System.Collections;
using System.Collections.Immutable;

namespace ZodSharp.SourceGenerators.Helpers;

partial class TypeLibrary
{
	public static readonly TypeIdentity CancellationToken = new(typeof(CancellationToken));

	public static readonly TypeIdentity ValueTask = new(typeof(ValueTask));

	public static class Collections
	{
		public static readonly TypeIdentity ImmutableArray = new(typeof(ImmutableArray));

		public static readonly TypeIdentity List = new(typeof(List<>));

		public static readonly TypeIdentity ICollection = new(typeof(ICollection));

		public static readonly TypeIdentity IEnumerable = new(typeof(IEnumerable));

		public static readonly TypeIdentity ICollectionT = new(typeof(ICollection<>));

		public static readonly TypeIdentity IEnumerableT = new(typeof(IEnumerable<>));

		public static readonly TypeIdentity IReadOnlyCollectionT = new(typeof(IReadOnlyCollection<>));
	}
}
