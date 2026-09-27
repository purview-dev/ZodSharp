using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators;

public partial class ZodSchemaAnalyzerTests
{
	[Test]
	public async Task ZodRefinementHook_GivenImplementationOnPartialType_ProducesNoDiagnostics(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System.ComponentModel.DataAnnotations;
			using ZodSharp.Core;
			using ZodSharp.Schemas;

			namespace Testing
			{
				[ZodSchema]
				public partial class Paging
				{
					[Range(1, 100)]
					public int DefaultPageSize { get; set; } = 50;

					// Mirrors the declaration the generator emits into the target type. Analyzers run after
					// generation, so this is visible alongside the caller's implementation.
					partial void OnZodValidate(RefineCtx<Paging> context);

					partial void OnZodValidate(RefineCtx<Paging> context)
					{
						if (context.Value.DefaultPageSize > 100)
							context.AddIssue("custom", "Too big.", [nameof(DefaultPageSize)]);
					}
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task ZodRefinementHook_GivenImplementationOnNonPartialType_ProducesZODSGEN034(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using ZodSharp.Schemas;

			namespace Testing
			{
				[ZodSchema]
				public class Paging
				{
					public int DefaultPageSize { get; set; } = 50;

					partial void OnZodValidate(RefineCtx<Paging> context)
					{
						context.AddIssue("custom", "Too big.", [nameof(DefaultPageSize)]);
					}
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ZodRefinementHookTypeNotPartial);
	}

	[Test]
	public async Task ZodRefinementHook_GivenNonPartialDeclaration_ProducesZODSGEN035(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using ZodSharp.Schemas;

			namespace Testing
			{
				[ZodSchema]
				public partial class Paging
				{
					public int DefaultPageSize { get; set; } = 50;

					public void OnZodValidate(RefineCtx<Paging> context)
					{
						context.AddIssue("custom", "Too big.", [nameof(DefaultPageSize)]);
					}
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ZodRefinementHookInvalidSignature);
	}

	[Test]
	public async Task ZodRefinementHook_GivenParameterlessDeclaration_ProducesZODSGEN035(
		CancellationToken cancellationToken
	)
	{
		var source = """
			namespace Testing
			{
				[ZodSchema]
				public partial class Paging
				{
					public int DefaultPageSize { get; set; } = 50;

					partial void OnZodValidate()
					{
					}
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ZodRefinementHookInvalidSignature);
	}

	[Test]
	public async Task SyncRefinement_GivenRetiredValidateMethod_ProducesZODSGEN036(CancellationToken cancellationToken)
	{
		var source = """
			using System.Collections.Generic;
			using ZodSharp.Core;

			namespace Testing
			{
				[ZodSchema]
				public class Paging
				{
					public int DefaultPageSize { get; set; } = 50;

					public IEnumerable<ValidationError> Validate() => [];
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.SyncRefinementMethodRetired);
	}

	[Test]
	public async Task SyncRefinement_GivenUnrelatedValidateMethod_ProducesNoDiagnostics(
		CancellationToken cancellationToken
	)
	{
		var source = """
			namespace Testing
			{
				[ZodSchema]
				public class Paging
				{
					public int DefaultPageSize { get; set; } = 50;

					public bool Validate() => true;
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task ZodRefinementHook_GivenDeclarationWithoutImplementation_ProducesNoDiagnostics(
		CancellationToken cancellationToken
	)
	{
		// Mirrors a target type that leaves the generated declaration in place and supplies no body: the
		// declaration must not be mistaken for a malformed hook.
		const string source = """
			using ZodSharp.Schemas;

			namespace Testing
			{
				[ZodSchema]
				public partial class Paging
				{
					public int DefaultPageSize { get; set; } = 50;

					partial void OnZodValidate(RefineCtx<Paging> context);
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task ZodRefinementHook_GivenNoHook_ProducesNoDiagnostics(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[ZodSchema]
				public partial class Paging
				{
					public int DefaultPageSize { get; set; } = 50;
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasNoDiagnostics();
	}
}
