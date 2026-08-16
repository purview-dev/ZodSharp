using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Infra;

public abstract class IncrementalSourceGeneratorTestBase<TGenerator>(bool throwOnLogError = true)
	: TUnitSourceGeneratorTestBase<TGenerator>
	where TGenerator : class, IIncrementalGenerator, new()
{
	static readonly Type[] DefaultAssemblyTypes =
	[
		typeof(Z),
		typeof(ImmutableArray),
		typeof(RequiredAttribute),
		typeof(System.Text.Json.JsonSerializer),
		typeof(System.Text.RegularExpressions.Regex),
	];

	public static readonly string[] GeneratedAttributes = ["EmbeddedAttribute.cs", "ZodSchemaAttribute.g.cs"];

	public static readonly int ExpectedFileCount = GeneratedAttributes.Length;
	public static readonly int ExpectedFileCountPlusGen = ExpectedFileCount + 1;

	public const int HintNameHashHexLength = 16;
	public const string GeneratedSourceFileSuffix = ".g.cs";

	protected async Task<DriverRunResult> GenerateZodAsync(string source, CancellationToken cancellationToken) =>
		await GenerateZodAsync(source, GenerationDriverContext.Default, cancellationToken);

	protected override async Task OnAfterRunAsync(
		DriverRunResult result,
		IEnumerable<string> sources,
		SourceGeneratorTestOptions options,
		CancellationToken cancellationToken
	)
	{
		var context = (GenerationDriverContext)options.State!;
		if (context.EnsureValid)
			result.EnsureValid();

		if (context.ValidateNoErrorDiagnostics)
			await Assert.That(result).HasNoErrorDiagnostics();
	}

	protected async Task<DriverRunResult> GenerateZodAsync(
		string source,
		GenerationDriverContext driverContext,
		CancellationToken cancellationToken
	)
	{
		SourceGeneratorTestOptions options = new()
		{
			IncludeDefaultNamespaces = driverContext.IncludeNamespaces,
			AdditionalNamespaces = [TypeLibrary.ZodSharpNamespace],
			AdditionalAssemblyTypes = ImmutableArray.Create(DefaultAssemblyTypes),
			ThrowOnGenerationException = driverContext.ThrowOnGenerationException,
			CompileToAssembly = driverContext.CompileToAssembly,
			ThrowOnLogError = throwOnLogError,
			DisableSourceGeneratorPropertyName = PropertyLibrary.DisableZodSharpSourceGeneratorProperty,
			DisableSourceGeneratorValue = driverContext.DisableSourceGenerator,
			PreprocessReferences = driverContext.PreprocessReferences,
			ExcludeGeneratedAttributes = ImmutableArray.Create(GeneratedAttributes),
			State = driverContext,
		};

		return await GenerateAsync(source, options, cancellationToken);
	}
}
