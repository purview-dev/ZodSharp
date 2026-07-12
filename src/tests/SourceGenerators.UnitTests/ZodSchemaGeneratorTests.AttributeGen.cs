namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGeneratorTests
{
	[Test]
	public async Task Generate_GivenEmptySource_GeneratesAttributesOnly(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	public class Empty { }
}
";

		// Act
		var (result, _) = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result.GeneratedTrees).Count().IsEqualTo(ExpectedFileCount);
	}

	[Test]
	public async Task Generate_GivenAttributeFiles_ContainsGenerateZodAttributes(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	public class Empty { }
}
";

		// Act
		var (result, _) = await GenerateAsync(source, cancellationToken);

		// Assert — attribute files are generated
		var attributeSources = result.GeneratedTrees.Select(t => t.GetText().ToString()).ToList();

		await Assert.That(attributeSources).Count().IsEqualTo(ExpectedFileCount);

		var allAttributeSource = string.Join("\n", attributeSources);
		await Assert.That(allAttributeSource).Contains("class EmbeddedAttribute");
		await Assert.That(allAttributeSource).Contains("class ZodSchemaAttribute");
	}
}
