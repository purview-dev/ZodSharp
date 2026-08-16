using System.Collections;
using System.Collections.Immutable;

namespace ZodSharp.SourceGenerators.Helpers;

partial class TypeLibrary
{
	public static readonly TypeValueObject CancellationToken = new(typeof(CancellationToken));

	public static readonly TypeValueObject ValueTask = new(typeof(ValueTask));

	public static class Collections
	{
		public static readonly TypeValueObject ImmutableArray = new(typeof(ImmutableArray));

		public static readonly TypeValueObject List = new(typeof(List<>));

		public static readonly TypeValueObject ICollection = new(typeof(ICollection));

		public static readonly TypeValueObject IEnumerable = new(typeof(IEnumerable));

		public static readonly TypeValueObject ICollectionT = new(typeof(ICollection<>));

		public static readonly TypeValueObject IEnumerableT = new(typeof(IEnumerable<>));

		public static readonly TypeValueObject IReadOnlyCollectionT = new(typeof(IReadOnlyCollection<>));
	}
}
