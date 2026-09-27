using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators;

public partial class ZodSchemaAnalyzerTests
{
	[Test]
	public async Task ValidationMethods_GivenHookAndAsync_ProducesZODSGEN029(CancellationToken cancellationToken)
	{
		var source = """
			using System.Threading;
			using System.Threading.Tasks;
			using ZodSharp.Core;
			using ZodSharp.Schemas;

			namespace Testing
			{
				[ZodSchema]
				public partial class Both
				{
					public string? Name { get; set; }

					partial void OnZodValidate(RefineCtx<Both> context)
					{
						context.AddIssue("custom", "Nope.", [nameof(Name)]);
					}

					internal static ValueTask<ValidationResult<Both>> CustomValidationAsync(
						Both value, CancellationToken ct) =>
						ValueTask.FromResult(ValidationResult<Both>.Success(value));
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AmbiguousValidationMethods);
	}

	[Test]
	public async Task ValidationMethods_GivenHookOnModelAndAsyncOnSchemaValidator_ProducesZODSGEN029(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System.Threading;
			using System.Threading.Tasks;
			using ZodSharp.Core;
			using ZodSharp.Schemas;

			namespace Testing
			{
				[ZodSchema]
				public partial class Both
				{
					public string? Name { get; set; }

					partial void OnZodValidate(RefineCtx<Both> context)
					{
						context.AddIssue("custom", "Nope.", [nameof(Name)]);
					}
				}

				public partial class BothSchemaValidator
				{
					public ValueTask<ValidationResult<Both>> CustomValidationAsync(
						Both value, CancellationToken ct) =>
						ValueTask.FromResult(ValidationResult<Both>.Success(value));
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AmbiguousValidationMethods);
	}

	[Test]
	public async Task ValidationMethods_GivenOnlyHookMethod_HasNoDiagnostics(CancellationToken cancellationToken)
	{
		var source = """
			using ZodSharp.Schemas;

			namespace Testing
			{
				[ZodSchema]
				public partial class OnlyHook
				{
					public string? Name { get; set; }

					partial void OnZodValidate(RefineCtx<OnlyHook> context)
					{
						context.AddIssue("custom", "Nope.", [nameof(Name)]);
					}
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task ValidationMethods_GivenOnlyAsyncMethod_HasNoDiagnostics(CancellationToken cancellationToken)
	{
		var source = """
			using System.Threading;
			using System.Threading.Tasks;
			using ZodSharp.Core;

			namespace Testing
			{
				[ZodSchema]
				public class OnlyAsync
				{
					public string? Name { get; set; }

					internal static ValueTask<ValidationResult<OnlyAsync>> CustomValidationAsync(
						OnlyAsync value, CancellationToken ct) =>
						ValueTask.FromResult(ValidationResult<OnlyAsync>.Success(value));
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasNoDiagnostics();
	}
}
