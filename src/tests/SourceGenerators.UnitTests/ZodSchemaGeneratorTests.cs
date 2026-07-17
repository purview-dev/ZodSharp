using System.Reflection;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Infra;

namespace ZodSharp.SourceGenerators;

public partial class ZodSchemaGeneratorTests : SourceGeneratorTestBase<ZodSchemaGenerator>
{
	static string GetSchemaGeneratedSource(GeneratorDriverRunResult result)
	{
		var syntaxTree = result.GeneratedTrees.FirstOrDefault(tree =>
		{
			var source = tree.GetText().ToString();
			return source.Contains(" static partial class ", StringComparison.Ordinal)
				&& source.Contains("Schema", StringComparison.Ordinal);
		});

		return syntaxTree?.GetText().ToString() ?? string.Empty;
	}

	static string GetSchemaGeneratedSource(GeneratorDriverRunResult result, string schemaName)
	{
		var syntaxTree = result.GeneratedTrees.FirstOrDefault(tree =>
		{
			var source = tree.GetText().ToString();
			return source.Contains($" static partial class {schemaName}", StringComparison.Ordinal);
		});

		return syntaxTree?.GetText().ToString() ?? string.Empty;
	}

	static async Task AssertNoGeneratorExceptions(GeneratorDriverRunResult result)
	{
		foreach (var genResult in result.Results)
		{
			await Assert.That(genResult.Exception).IsNull().Because(genResult.Exception?.Message!);
		}
	}

	static async Task AssertNoCompilationErrors(Compilation compilation, CancellationToken cancellationToken)
	{
		var errors = compilation
			.GetDiagnostics(cancellationToken)
			.Where(static d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		await Assert
			.That(errors)
			.IsEmpty()
			.Because("Errors:\n\t" + string.Join("\t", errors.Select(e => e.ToString() + Environment.NewLine)));
	}

	static async Task<Assembly> CompileToAssemblyAsync(Compilation compilation, CancellationToken cancellationToken)
	{
		await using MemoryStream assemblyStream = new();
		var emitResult = compilation.Emit(assemblyStream, cancellationToken: cancellationToken);
		if (!emitResult.Success)
		{
			var diagnostics = string.Join(
				Environment.NewLine,
				emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString())
			);

			throw new InvalidOperationException(diagnostics);
		}

		assemblyStream.Position = 0;
		return System.Reflection.Assembly.Load(assemblyStream.ToArray());
	}

	static Diagnostic[] GetGeneratorDiagnostics(GeneratorDriverRunResult result) =>
		[.. result.Results.SelectMany(static generatorResult => generatorResult.Diagnostics).OrderBy(static d => d.Id)];
}
