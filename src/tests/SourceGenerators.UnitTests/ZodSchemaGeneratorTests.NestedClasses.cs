using System.Reflection;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGeneratorTests
{
	[Test]
	public async Task Generate_GivenPrivateNestedClass_GeneratesCompilingSchema(CancellationToken cancellationToken)
	{
		const string source =
			@"
namespace Testing
{
	public partial class Outer
	{
		[ZodSchema]
		private class Inner
		{
			public string? Name { get; set; }
		}
	}
}
";

		var driverResult = await GenerateZodAsync(source, cancellationToken);
		var generatedSource = GetSchemaGeneratedSource(driverResult, "InnerSchema");

		await Assert.That(generatedSource).Contains("private static partial class InnerSchema");
		await Assert.That(generatedSource).Contains("public partial class Outer");
	}

	[Test]
	public async Task Generate_GivenMultiLevelNestedClass_GeneratesCompilingSchema(CancellationToken cancellationToken)
	{
		const string source =
			@"
namespace Testing
{
	public partial class Outer
	{
		public partial class Middle
		{
			[ZodSchema]
			private class Inner
			{
				public string? Name { get; set; }
			}
		}
	}
}
";

		var driverResult = await GenerateZodAsync(source, cancellationToken);

		await Assert.That(GetSchemaGeneratedSource(driverResult, "InnerSchema")).IsNotEmpty();
	}

	[Test]
	public async Task NestedClass_Runtime_ValidatesNestedSchema(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.ComponentModel.DataAnnotations;
using ZodSharp;

namespace Testing
{
	public partial class Outer
	{
		[ZodSchema]
		private class Inner
		{
			[Required]
			[StringLength(10, MinimumLength = 2)]
			public string Name { get; set; } = string.Empty;
		}
	}
}";

		var driverResult = await GenerateZodAsync(source, cancellationToken);
		var assembly = await Assert.That(driverResult.Assembly).IsNotNull();

		var outerType = assembly.GetType("Testing.Outer")!;
		var innerType = outerType.GetNestedType("Inner", BindingFlags.NonPublic)!;
		var schemaType = outerType.GetNestedType("InnerSchema", BindingFlags.NonPublic)!;

		var instance = Activator.CreateInstance(innerType)!;
		innerType.GetProperty("Name")!.SetValue(instance, "A");

		var validateMethod = schemaType.GetMethod("Validate")!;
		var result = validateMethod.Invoke(null, [instance])!;
		var isSuccess = (bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!;

		await Assert.That(isSuccess).IsFalse();
	}
}
