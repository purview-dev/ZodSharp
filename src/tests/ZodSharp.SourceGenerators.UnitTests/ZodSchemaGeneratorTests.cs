using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Infra;

namespace ZodSharp.SourceGenerators;

public partial class ZodSchemaGeneratorTests : SourceGeneratorTestBase<ZodSchemaGenerator>
{
	static string GetSchemaGeneratedSource(GeneratorDriverRunResult result)
	{
		var aggregateTree = result.GeneratedTrees.FirstOrDefault(tree =>
		{
			var source = tree.GetText().ToString();
			return source.Contains(" static partial class ", StringComparison.Ordinal)
				&& source.Contains("Schema", StringComparison.Ordinal);
		});

		return aggregateTree?.GetText().ToString() ?? string.Empty;
	}

	static string GetSchemaGeneratedSource(GeneratorDriverRunResult result, string schemaName)
	{
		var aggregateTree = result.GeneratedTrees.FirstOrDefault(tree =>
		{
			var source = tree.GetText().ToString();
			return source.Contains($" static partial class {schemaName}", StringComparison.Ordinal);
		});

		return aggregateTree?.GetText().ToString() ?? string.Empty;
	}

	static async Task AssertNoGeneratorExceptions(GeneratorDriverRunResult result)
	{
		foreach (var genResult in result.Results)
		{
			await Assert.That(genResult.Exception).IsNull();
		}
	}

	static async Task AssertNoCompilationErrors(Compilation compilation, CancellationToken cancellationToken)
	{
		var errors = compilation
			.GetDiagnostics(cancellationToken)
			.Where(static d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		await Assert.That(errors).IsEmpty();
	}

	static Diagnostic[] GetGeneratorDiagnostics(GeneratorDriverRunResult result) =>
		[.. result.Results.SelectMany(static generatorResult => generatorResult.Diagnostics).OrderBy(static d => d.Id)];
}
