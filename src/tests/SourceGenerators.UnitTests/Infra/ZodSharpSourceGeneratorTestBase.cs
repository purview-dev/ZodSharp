using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Infra;

public abstract class ZodSharpSourceGeneratorTestBase<TGenerator>
	: TUnitSourceGeneratorTestBase<TGenerator, ZodSourceGeneratorTestOptions>
	where TGenerator : class, IIncrementalGenerator, new()
{
	public static readonly string[] GeneratedAttributes = ["EmbeddedAttribute.cs", "ZodSchemaAttribute.g.cs"];

	public static readonly int ExpectedFileCount = GeneratedAttributes.Length;

	public static readonly int ExpectedFileCountPlusGen = ExpectedFileCount + 1;

	public const int HintNameHashHexLength = 16;

	public const string GeneratedSourceFileSuffix = ".g.cs";
}
