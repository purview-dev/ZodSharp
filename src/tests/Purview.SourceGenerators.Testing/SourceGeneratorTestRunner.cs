using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.SourceGenerators.Testing.Abstractions;

namespace Purview.SourceGenerators.Testing;

/// <summary>
/// Executes a source generator against a test compilation and returns the result.
/// </summary>
public sealed class SourceGeneratorTestRunner<TGenerator>
	where TGenerator : class, IIncrementalGenerator, new()
{
	static readonly string[] TrustedAssemblies = (
		(string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? ""
	).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

	/// <summary>
	/// Runs the generator against the provided source using the specified options.
	/// </summary>
	public async Task<DriverRunResult> RunAsync(
		string source,
		SourceGeneratorTestOptions? options = null,
		CancellationToken cancellationToken = default
	)
	{
		options ??= new SourceGeneratorTestOptions();

		var logEntries = new List<(string Message, OutputType Type)>();

		source = PrepareSource(source, options);
		var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
		var references = ResolveReferences(options);
		var compilation = CreateCompilation(syntaxTree, references, options);
		var generator = new TGenerator();
		ConfigureLogging(generator, options, logEntries);
		var driver = CreateDriver(generator, options);
		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out _,
			cancellationToken
		);
		var result = driver.GetRunResult();

		Assembly? assembly = null;
		if (options.CompileToAssembly)
			assembly = await CompileToAssemblyAsync(outputCompilation, cancellationToken);

		var nonAttributeTrees = ExcludeGeneratedAttributes(result, options.ExcludeGeneratedAttributes);

		return new(result, outputCompilation, assembly, result.GeneratedTrees, nonAttributeTrees, logEntries);
	}

	static string PrepareSource(string source, SourceGeneratorTestOptions options)
	{
		if (!options.IncludeDefaultNamespaces)
			return source;

		var namespaces = options.DefaultNamespaces.AddRange(options.AdditionalNamespaces);
		var usings = string.Join(Environment.NewLine, namespaces.Select(n => $"using {n};"));

		return usings + Environment.NewLine + Environment.NewLine + source;
	}

	static CSharpCompilation CreateCompilation(
		SyntaxTree syntaxTree,
		ImmutableArray<MetadataReference> references,
		SourceGeneratorTestOptions options
	)
	{
		return CSharpCompilation.Create(
			options.CompilationAssemblyName,
			[syntaxTree],
			references,
			new CSharpCompilationOptions(options.OutputKind)
		);
	}

	static ImmutableArray<MetadataReference> ResolveReferences(SourceGeneratorTestOptions options)
	{
		var builder = ImmutableArray.CreateBuilder<MetadataReference>();
		builder.AddRange(TrustedAssemblies.Select(static p => MetadataReference.CreateFromFile(p)));
		builder.AddRange(
			options.AdditionalAssemblyTypes.Select(static a => MetadataReference.CreateFromFile(a.Assembly.Location))
		);
		builder.AddRange(options.AdditionalReferences);

		var references = builder.ToImmutable();
		options.PreprocessReferences?.Invoke(references);
		return references;
	}

	static GeneratorDriver CreateDriver(TGenerator generator, SourceGeneratorTestOptions options)
	{
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		var analyzerOptions = new Dictionary<string, string>(options.AnalyzerConfigOptions);
		if (options.DisableSourceGeneratorPropertyName is not null && options.DisableSourceGeneratorValue is not null)
			analyzerOptions[options.DisableSourceGeneratorPropertyName] =
				options.DisableSourceGeneratorValue.Value.ToString();

		if (analyzerOptions.Count > 0)
			driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(analyzerOptions));

		return driver;
	}

	static void ConfigureLogging(
		TGenerator generator,
		SourceGeneratorTestOptions options,
		List<(string, OutputType)> logEntries
	)
	{
		if (generator is ILogSupport logSupport)
		{
			logSupport.SetLogOutput(
				(message, type) =>
				{
					options.TestOutput.WriteLine($"[{type}] {message}");
					logEntries.Add((message, type));
				}
			);
			return;
		}

		var logSupportType = generator.GetType().GetInterfaces().FirstOrDefault(i => i.Name == "ILogSupport");
		var setLogOutput = logSupportType?.GetMethod("SetLogOutput");
		if (setLogOutput is null)
			return;

		var paramType = setLogOutput.GetParameters()[0].ParameterType;
		var genericArgs = paramType.GenericTypeArguments;
		if (genericArgs.Length < 2)
			throw new InvalidOperationException(
				$"SetLogOutput parameter type '{paramType}' does not have two generic arguments."
			);

		var outputTypeType = genericArgs[1];
		var actionType = typeof(Action<,>).MakeGenericType(typeof(string), outputTypeType);
		var factoryType = typeof(LogActionFactory<>).MakeGenericType(outputTypeType);
		var factory = Activator.CreateInstance(factoryType, options, logEntries);
		var actionMethod = factoryType.GetMethod(nameof(LogActionFactory<>.Action))!;
		var action = Delegate.CreateDelegate(actionType, factory, actionMethod);

		setLogOutput.Invoke(generator, [action]);
	}

	static async Task<Assembly?> CompileToAssemblyAsync(Compilation compilation, CancellationToken cancellationToken)
	{
		await using var assemblyStream = new MemoryStream();
		var emitResult = compilation.Emit(assemblyStream, cancellationToken: cancellationToken);
		if (!emitResult.Success)
			return null;

		assemblyStream.Position = 0;
		return Assembly.Load(assemblyStream.ToArray());
	}

	static IEnumerable<SyntaxTree> ExcludeGeneratedAttributes(
		GeneratorDriverRunResult result,
		ImmutableArray<string> exclude
	)
	{
		return exclude.IsEmpty
			? result.GeneratedTrees
			: result.GeneratedTrees.Where(tree =>
				!exclude.Any(attr => tree.FilePath.EndsWith(attr, StringComparison.Ordinal))
			);
	}
}

sealed class LogActionFactory<TOutputType>(SourceGeneratorTestOptions options, List<(string, OutputType)> logEntries)
{
	public void Action(string message, TOutputType type)
	{
		var outputType = (OutputType)Enum.ToObject(typeof(OutputType), type!);
		options.TestOutput.WriteLine($"[{outputType}] {message}");
		logEntries.Add((message, outputType));
	}
}
