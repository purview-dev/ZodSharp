using ZodSharp.Core;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGeneratorTests
{
	[Test]
	public async Task Generate_GivenZodSchema_AlsoEmitsValidatorAdapter(CancellationToken cancellationToken)
	{
		const string source =
			@"
namespace Testing
{
    [ZodSchema]
    public class Widget
    {
        public string? Name { get; set; }
    }
}";
		var (result, outputCompilation) = await GenerateAsync(source, cancellationToken);
		var generatedSource = GetSchemaGeneratedSource(result, "WidgetSchema");

		await AssertNoGeneratorExceptions(result);
		await AssertNoCompilationErrors(outputCompilation, cancellationToken);
		await Assert.That(generatedSource).Contains("class WidgetSchemaValidator");
		await Assert.That(generatedSource).Contains("IZodSchemaValidator");
		await Assert.That(generatedSource).Contains("Widget");
		await Assert.That(generatedSource).Contains("Validate(");
		await Assert.That(generatedSource).Contains("return WidgetSchema.Validate(value);");
	}

	[Test]
	public async Task Generate_GivenZodSchema_EmitsModuleAttribute(CancellationToken cancellationToken)
	{
		const string source =
			@"
namespace Testing
{
    [ZodSchema]
    public class Gadget { }
}";
		var (result, outputCompilation) = await GenerateAsync(source, cancellationToken);
		var allGenerated = string.Join("\n", result.GeneratedTrees.Select(t => t.GetText().ToString()));

		await AssertNoGeneratorExceptions(result);
		await AssertNoCompilationErrors(outputCompilation, cancellationToken);
		await Assert.That(allGenerated).Contains("ZodSchemaGenerated");
		await Assert.That(allGenerated).Contains("Gadget");
	}

	[Test]
	public async Task Generate_GivenZodSchema_ValidatorAdapterImplementsValidateAsync(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
    [ZodSchema]
    public class Gizmo
    {
        public string? Name { get; set; }
    }
}";
		var (result, outputCompilation) = await GenerateAsync(source, cancellationToken);
		var generatedSource = GetSchemaGeneratedSource(result, "GizmoSchema");

		await AssertNoGeneratorExceptions(result);
		await AssertNoCompilationErrors(outputCompilation, cancellationToken);
		await Assert.That(generatedSource).Contains("ValidateAsync");
		await Assert.That(generatedSource).Contains("ValueTask");
		await Assert.That(generatedSource).Contains("GizmoSchemaValidator");
	}

	[Test]
	public async Task Generate_GivenZodSchemaWithCustomAsyncValidator_ValidatorAdapterCallsCustomValidateAsync(
		CancellationToken cancellationToken
	)
	{
		var rnd = $"{Guid.NewGuid()}";

		var source =
			$@"
	using System.Threading;
	using System.Threading.Tasks;
	using ZodSharp.Core;

	namespace Testing
	{{
	    [ZodSchema]
	    public class Gizmo
	    {{
	        public string? Name {{ get; set; }}
	    }}

		partial class GizmoSchemaValidator
		{{
			async ValueTask<ValidationResult<Gizmo>> CustomValidationAsync(Gizmo value, CancellationToken cancellationToken default)
			{{
				throw new Exception(""{rnd}"");
			}}
		}}
	}}";
		var (result, outputCompilation) = await GenerateAsync(source, cancellationToken);

		await AssertNoGeneratorExceptions(result);
		await AssertNoCompilationErrors(outputCompilation, cancellationToken);
		await AssertNoDiagnostics(result);

		var assembly = await CompileToAssemblyAsync(outputCompilation, cancellationToken);

		var gizmoType = assembly.GetType("Testing.Gizmo");
		var gizmoSchemaType = assembly.GetType("Testing.GizmoSchemaValidator");

		await Assert.That(gizmoType).IsNotNull();
		await Assert.That(gizmoSchemaType).IsNotNull();

		var methods = gizmoSchemaType.GetMethods(
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
		);
		await Assert.That(methods).Contains(m => m.Name == "CustomValidationAsync");

		var gizmo = Activator.CreateInstance(gizmoType);
		var gizmoSchemaValidator = Activator.CreateInstance(gizmoSchemaType);

		var method = gizmoSchemaType.GetMethod(nameof(IZodSchemaValidator<>.ValidateAsync));
		await Assert.That(method).IsNotNull();

		var valueTaskResult = method!.Invoke(gizmoSchemaValidator, [gizmo, cancellationToken])!;

		void CallResult() => valueTaskResult.GetType().GetProperty("Result");

		await Assert.That(CallResult).Throws<Exception>().WithMessage(rnd, StringComparison.Ordinal);
	}
}
