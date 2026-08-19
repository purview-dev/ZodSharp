using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Infra;

public abstract class IncrementalSourceGeneratorTestBase<TGenerator>
	: TUnitSourceGeneratorTestBase<TGenerator, ZodSharpGeneratorTestOptions>
	where TGenerator : class, IIncrementalGenerator, new()
{
	protected static readonly int ExpectedFileCount = ZodSharpGeneratorTestOptions.GeneratedAttributes.Length;

	protected static readonly int ExpectedFileCountPlusGen = ExpectedFileCount + 1;

	protected const int HintNameHashHexLength = 16;

	protected const string GeneratedSourceFileSuffix = ".g.cs";
}
