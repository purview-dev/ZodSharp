using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Infra;

public abstract class SourceGeneratorTestBase<TGenerator>(bool throwOnLogError = true)
	where TGenerator : class, IIncrementalGenerator, new()
{
	public static readonly string[] GeneratedAttributes = ["EmbeddedAttribute.cs", "ZodSchema.g.cs"];

	public static readonly int ExpectedFileCount = GeneratedAttributes.Length;
	public static readonly int ExpectedFileCountPlusGen = ExpectedFileCount + 1;

	public const int HintNameHashHexLength = 16;
	public const string GeneratedSourceFileSuffix = ".g.cs";

	protected async Task<(GeneratorDriverRunResult Result, Compilation OutputCompilation)> GenerateAsync(
		string source,
		bool includeNamespaces,
		CancellationToken cancellationToken
	) => await GenerateAsync(source, includeNamespaces, null, cancellationToken);

	protected async Task<(GeneratorDriverRunResult Result, Compilation OutputCompilation)> GenerateAsync(
		string source,
		bool includeNamespaces,
		Action<List<MetadataReference>>? preprocessReferences,
		CancellationToken cancellationToken
	)
	{
		if (includeNamespaces)
		{
			source =
				@"
using System;
using System.Collections.Generic;
using System.Linq;

using ZodSharp;

" + source;
		}

		var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);

		List<MetadataReference> references =
		[
			// Make sure to include ZodSharp assembly reference
			MetadataReference.CreateFromFile(typeof(Z).Assembly.Location),
			// Other framework requirements...
			MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(RequiredAttribute).Assembly.Location),
			// System assemblies
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
			MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
			MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).Assembly.Location),
			// Add netstandard reference
			MetadataReference.CreateFromFile(
				System
					.Reflection.Assembly.Load(
						"netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"
					)
					.Location
			),
		];

		preprocessReferences?.Invoke(references);

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		TGenerator generator = new();

		if (generator is ILogSupport logging && TestContext.Current is not null)
		{
			logging.SetLogOutput(
				(message, outputType) =>
				{
					var prefix = outputType switch
					{
						OutputType.Diagnostic => "DIAG",
						OutputType.Debug => "DBUG",
						OutputType.Info => "INFO",
						OutputType.Warning => "WARN",
						OutputType.Error => "ERR",
						_ => "????",
					};

					TestContext.Current.OutputWriter.WriteLine($"{prefix}: {message}");

					if (throwOnLogError && outputType == OutputType.Error)
						throw new InvalidOperationException($"Generator logged error: {message}");
				}
			);
		}

		var driver = CSharpGeneratorDriver
			.Create(generator)
			.RunGeneratorsAndUpdateCompilation(
				compilation,
				out var outputCompilation,
				out var diagnostics,
				cancellationToken
			);

		var result = driver.GetRunResult();

		// No generator exceptions
		foreach (var genResult in result.Results)
		{
			if (genResult.Exception is not null)
				throw genResult.Exception;
		}

		return (result, outputCompilation);
	}

	protected async Task<(GeneratorDriverRunResult Result, Compilation OutputCompilation)> GenerateAsync(
		string source,
		CancellationToken cancellationToken
	) => await GenerateAsync(source, includeNamespaces: true, cancellationToken);

	protected async Task<Assembly> CompileToAssemblyAsync(string source, CancellationToken cancellationToken)
	{
		var (_, compilation) = await GenerateAsync(source, cancellationToken);
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

	protected static IEnumerable<SyntaxTree> ExcludeGenAttribs(GeneratorDriverRunResult result)
	{
		return result.GeneratedTrees.Where(tree =>
			!GeneratedAttributes.Any(attr => tree.FilePath.EndsWith(attr, StringComparison.Ordinal))
		);
	}
}
