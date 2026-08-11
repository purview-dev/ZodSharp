namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGeneratorTests
{
	[Test]
	public async Task Generate_GivenModel_GeneratedSchemaHasExpectedStructure(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[ZodSchema]
	public class Customer
	{
		public string? Name { get; set; }
	}
}
";

		// Act
		var driverResult = await GenerateZodAsync(source, cancellationToken);
		var generatedSource = GetSchemaGeneratedSource(driverResult, "CustomerSchema");

		// Assert
		await Assert.That(generatedSource).Contains("namespace Testing");
		await Assert.That(generatedSource).Contains("#nullable enable");
		await Assert.That(generatedSource).Contains("public static partial class CustomerSchema");
	}

	[Test]
	public async Task Generate_GivenClassWithoutZodSchema_DoesNotGenerateSchema(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	public class Customer { }
}
";

		// Act
		var driverResult = await GenerateZodAsync(source, cancellationToken);
		var generatedSource = GetSchemaGeneratedSource(driverResult, "CustomerSchema");

		// Assert
		await Assert.That(generatedSource).IsEmpty();
	}

	[Test]
	public async Task Generate_GivenMultipleAnnotatedTypes_GeneratesAllSchemas(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[ZodSchema]
	public class Customer { }

	[ZodSchema]
	public struct Address { }

	[ZodSchema]
	public record Order { }
}
";

		// Act
		var driverResult = await GenerateZodAsync(source, cancellationToken);

		// Assert
		await Assert.That(GetSchemaGeneratedSource(driverResult, "CustomerSchema")).IsNotEmpty();
		await Assert.That(GetSchemaGeneratedSource(driverResult, "AddressSchema")).IsNotEmpty();
		await Assert.That(GetSchemaGeneratedSource(driverResult, "OrderSchema")).IsNotEmpty();
	}
}
