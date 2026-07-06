using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Infra;

namespace ZodSharp.SourceGenerators;

public partial class ZodSchemaGeneratorTests : SourceGeneratorTestBase<ZodSchemaGenerator>
{
	static string GetSchemaGeneratedSource(GeneratorDriverRunResult result)
	{
		var aggregateTree = ExcludeGenAttribs(result).FirstOrDefault();

		return aggregateTree?.GetText().ToString() ?? string.Empty;
	}

	static Diagnostic[] GetGeneratorDiagnostics(GeneratorDriverRunResult result) =>
		[.. result.Results.SelectMany(static generatorResult => generatorResult.Diagnostics).OrderBy(static d => d.Id)];
}
