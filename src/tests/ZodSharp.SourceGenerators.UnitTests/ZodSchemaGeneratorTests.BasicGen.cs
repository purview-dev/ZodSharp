using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGeneratorTests
{
	[Test]
	public async Task Generate_GivenEmptyZodSchema_OutputCompilationHasNoErrors(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[ZodSchema]
	class BasicModel { }
}
";

		// Act
		var (result, outputCompilation) = await GenerateAsync(source, cancellationToken);

		// Assert — no generator exceptions
		foreach (var genResult in result.Results)
		{
			await Assert.That(genResult.Exception).IsNull();
		}

		// Assert — no compilation errors
		var errors = outputCompilation
			.GetDiagnostics(cancellationToken)
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		await Assert.That(errors).IsEmpty();
	}

	[Test]
	[Arguments("public")]
	[Arguments("private")]
	[Arguments("internal")]
	// This is internal too
	[Arguments("")]
	public async Task Generate_GivenClassModifier_GeneratesSchemaWithSameModifier(
		string modifier,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var expectation = $"{modifier} static partial class ModifierTestSchema";

		var source =
			$@"
namespace Testing
{{
	[ZodSchema]
	{modifier} class ModifierTest {{ }}
}}
";

		// Act
		var (result, _) = await GenerateAsync(source, cancellationToken);
		var generatedSource = GetSchemaGeneratedSource(result);

		// Assert — generated file starts with auto-generated header
		await Assert.That(generatedSource).Contains(expectation);
	}
}
