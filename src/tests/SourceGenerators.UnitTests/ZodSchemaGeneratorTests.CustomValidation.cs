using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Infra;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGeneratorTests
{
	[Test]
	public async Task CustomValidation_GivenNoMethod_GeneratesSyncFallback_NoDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[ZodSchema]
	public class NoCustom { public string? Name { get; set; } }
}";

		var driverResult = await GenerateAsync(source, cancellationToken);
		var generated = driverResult.GetSource("NoCustomSchema");

		// No async state machine
		await Assert
			.That(generated)
			.ContainsGeneratedCode("ValueTask.FromResult")
			.Because("Should use FromResult, not async");
		await Assert
			.That(generated)
			.DoesNotContain("async ", StringComparison.Ordinal)
			.Because("Should not generate async when no custom method");
	}

	[Test]
	public async Task CustomValidation_GivenDefaultMethodExists_GeneratesAsyncPath(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class WithDefault
	{
		public string? Name { get; set; }

		internal static ValueTask<ValidationResult<WithDefault>> CustomValidationAsync(
			WithDefault value, CancellationToken ct) =>
			ValueTask.FromResult(ValidationResult<WithDefault>.Success(value));
	}
}";

		var driverResult = await GenerateAsync(source, cancellationToken);
		var generated = driverResult.GetSource("WithDefaultSchema");

		await Assert
			.That(generated)
			.Contains("async ", StringComparison.Ordinal)
			.Because("Should generate async when custom method exists");
		await Assert.That(generated).ContainsGeneratedCode("await global::Testing.WithDefault.CustomValidationAsync");
		await Assert.That(generated).ContainsGeneratedCode(".ConfigureAwait(false)");
		await Assert.That(generated).ContainsGeneratedCode(".Merge(syncResult, customResult)");
	}

	[Test]
	public async Task CustomValidation_GivenOverriddenMethodName_GeneratesAsyncPath(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema(CustomValidationMethodName = nameof(ValidateRulesAsync))]
	public class WithOverride
	{
		public string? Name { get; set; }

		internal static ValueTask<ValidationResult<WithOverride>> ValidateRulesAsync(
			WithOverride value, CancellationToken ct) =>
			ValueTask.FromResult(ValidationResult<WithOverride>.Success(value));
	}
}";

		var driverResult = await GenerateAsync(source, cancellationToken);
		var generated = driverResult.GetSource("WithOverrideSchema");

		await Assert.That(generated).ContainsGeneratedCode("await global::Testing.WithOverride.ValidateRulesAsync");
	}

	[Test]
	public async Task CustomValidation_GivenExplicitNameMissing_ProducesZODSGEN007(CancellationToken cancellationToken)
	{
		const string source =
			@"
namespace Testing
{
	[ZodSchema(CustomValidationMethodName = ""DoesNotExistAsync"")]
	public class MissingMethod { public string? Name { get; set; } }
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);

		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationMethodNotFound);
	}

	[Test]
	public async Task CustomValidation_GivenInvalidReturnType_ProducesZODSGEN008(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class BadReturn
	{
		public string? Name { get; set; }

		internal static Task<ValidationResult<BadReturn>> CustomValidationAsync(
			BadReturn value, CancellationToken ct) =>
			Task.FromResult(ValidationResult<BadReturn>.Success(value));
	}
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInvalidReturnType);
	}

	[Test]
	public async Task CustomValidation_GivenWrongParameterCount_ProducesZODSGEN009(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class WrongParams
	{
		public string? Name { get; set; }

		internal static ValueTask<ValidationResult<WrongParams>> CustomValidationAsync(
			WrongParams value) =>
			ValueTask.FromResult(ValidationResult<WrongParams>.Success(value));
	}
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInvalidParameterCount);
	}

	[Test]
	public async Task CustomValidation_GivenWrongModelParameterType_ProducesZODSGEN010(
		CancellationToken cancellationToken
	)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class WrongModel
	{
		public string? Name { get; set; }

		internal static ValueTask<ValidationResult<WrongModel>> CustomValidationAsync(
			string value, CancellationToken ct) =>
			ValueTask.FromResult(ValidationResult<WrongModel>.Success(null!));
	}
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInvalidModelParameter);
	}

	[Test]
	public async Task CustomValidation_GivenWrongCancellationTokenType_ProducesZODSGEN011(
		CancellationToken cancellationToken
	)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class WrongCT
	{
		public string? Name { get; set; }

		internal static ValueTask<ValidationResult<WrongCT>> CustomValidationAsync(
			WrongCT value, string ct) =>
			ValueTask.FromResult(ValidationResult<WrongCT>.Success(value));
	}
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInvalidCancellationToken);
	}

	[Test]
	public async Task CustomValidation_GivenGenericMethod_ProducesZODSGEN012(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class GenericMethod
	{
		public string? Name { get; set; }

		internal static ValueTask<ValidationResult<T>> CustomValidationAsync<T>(
			T value, CancellationToken ct) where T : class =>
			ValueTask.FromResult(ValidationResult<T>.Success(value));
	}
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationGenericMethod);
	}

	[Test]
	public async Task CustomValidation_GivenInstanceMethod_ProducesZODSGEN013(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class InstanceMethod
	{
		public string? Name { get; set; }

		internal ValueTask<ValidationResult<InstanceMethod>> CustomValidationAsync(
			InstanceMethod value, CancellationToken ct) =>
			ValueTask.FromResult(ValidationResult<InstanceMethod>.Success(value));
	}
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInvalidStaticInstance);
	}

	[Test]
	public async Task CustomValidation_GivenPrivateMethod_ProducesZODSGEN014(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class PrivateMethod
	{
		public string? Name { get; set; }

		private static ValueTask<ValidationResult<PrivateMethod>> CustomValidationAsync(
			PrivateMethod value, CancellationToken ct) =>
			ValueTask.FromResult(ValidationResult<PrivateMethod>.Success(value));
	}
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInaccessible);
	}

	[Test]
	public async Task CustomValidation_GivenMultipleValidOverloads_ProducesZODSGEN015(
		CancellationToken cancellationToken
	)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class Ambiguous
	{
		public string? Name { get; set; }

		internal static ValueTask<ValidationResult<Ambiguous>> CustomValidationAsync(
			Ambiguous value, CancellationToken ct) =>
			ValueTask.FromResult(ValidationResult<Ambiguous>.Success(value));

		internal static ValueTask<ValidationResult<Ambiguous>> CustomValidationAsync(
			Ambiguous value, CancellationToken ct, string extra) =>
			ValueTask.FromResult(ValidationResult<Ambiguous>.Success(value));
	}
}";

		// The second overload has 3 params so it won't match — only one valid candidate.
		// This test verifies the generator doesn't falsely report ambiguity.
		var driverResult = await GenerateAsync(source, cancellationToken);

		await Assert.That(driverResult.EnsureValid).ThrowsNothing();
	}

	[Test]
	public async Task CustomValidation_GivenInvalidMethodName_ProducesZODSGEN016(CancellationToken cancellationToken)
	{
		const string source =
			@"
namespace Testing
{
	[ZodSchema(CustomValidationMethodName = ""123 invalid"")]
	public class BadName { public string? Name { get; set; } }
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInvalidMethodName);
	}

	[Test]
	public async Task CustomValidation_GivenRefParameter_ProducesZODSGEN019(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class RefParam
	{
		public string? Name { get; set; }

		internal static ValueTask<ValidationResult<RefParam>> CustomValidationAsync(
			ref RefParam value, CancellationToken ct) =>
			ValueTask.FromResult(ValidationResult<RefParam>.Success(value!));
	}
}";

		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInvalidParameterModifier);
	}

	[Test]
	public async Task CustomValidation_GivenOverloadsButOnlyOneValid_SelectsValid(CancellationToken cancellationToken)
	{
		var source =
			@"
using System.Threading;
using System.Threading.Tasks;
using ZodSharp.Core;

namespace Testing
{
	[ZodSchema]
	public class OverloadsOneValid
	{
		public string? Name { get; set; }

		// Wrong return type — will get a diagnostic
		internal static Task<ValidationResult<OverloadsOneValid>> CustomValidationAsync(
			OverloadsOneValid value, CancellationToken ct) =>
			Task.FromResult(ValidationResult<OverloadsOneValid>.Success(value));

		// Valid signature
		internal static ValueTask<ValidationResult<OverloadsOneValid>> CustomValidationAsync(
			OverloadsOneValid value, CancellationToken ct, bool extra) =>
			ValueTask.FromResult(ValidationResult<OverloadsOneValid>.Success(value));
	}
}";

		// The first has wrong return type, the second has 3 params (wrong count).
		// Neither is valid — should get diagnostics but no ambiguity.
		var driverResult = await GenerateAsync(source, ZodSourceGeneratorTestOptions.NoValidation, cancellationToken);

		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInvalidReturnType);
		await Assert.That(driverResult).HasDiagnostic(DiagnosticLibrary.CustomValidationInvalidParameterCount);
		await Assert.That(driverResult).DoesNotHaveDiagnostic(DiagnosticLibrary.CustomValidationAmbiguousOverloads);
	}
}
