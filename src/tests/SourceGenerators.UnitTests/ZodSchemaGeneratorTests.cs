using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing;
using ZodSharp.SourceGenerators.Infra;

namespace ZodSharp.SourceGenerators;

[Retry(3)]
public partial class ZodSchemaGeneratorTests : IncrementalSourceGeneratorTestBase<ZodSchemaGenerator>
{
	static string GetSchemaGeneratedSource(DriverRunResult driverRunResults) =>
		GetSchemaGeneratedSource(driverRunResults.Result);

	static string GetSchemaGeneratedSource(GeneratorDriverRunResult result)
	{
		var syntaxTree = result.GeneratedTrees.FirstOrDefault(static tree =>
		{
			var source = tree.GetText().ToString();
			return source.Contains(" static partial class ", StringComparison.Ordinal)
				&& source.Contains("Schema", StringComparison.Ordinal);
		});

		return syntaxTree?.GetText().ToString() ?? string.Empty;
	}

	static string GetSchemaGeneratedSource(DriverRunResult driverRunResults, string schemaName) =>
		GetSchemaGeneratedSource(driverRunResults.Result, schemaName);

	static string GetSchemaGeneratedSource(GeneratorDriverRunResult result, string schemaName)
	{
		var syntaxTree = result.GeneratedTrees.FirstOrDefault(tree =>
		{
			var source = tree.GetText().ToString();
			return source.Contains($" static partial class {schemaName}", StringComparison.Ordinal);
		});

		return syntaxTree?.GetText().ToString() ?? string.Empty;
	}
}
