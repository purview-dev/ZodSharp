using ZodSharp.SourceGenerators.Infra;

namespace ZodSharp.SourceGenerators;

public class ValidationRuleConventionsAnalyzerTests : ValidationRuleConventionsAnalyzerTestBase
{
	[Test]
	public async Task GivenRuleWithBothConstants_HasNoDiagnostics(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing;

			public readonly record struct PositiveRule : ZodSharp.Core.IValidationRule<double>
			{
				public const string ErrorCode = "too_small";
				public const string MessageFormat = "Value must be positive, but got {0}";

				public bool IsValid(in double value) => value > 0;

				public string GetErrorMessage(in double value) => string.Format(MessageFormat, value);
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task GivenRuleMissingBothConstants_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing;

			public readonly record struct BareRule : ZodSharp.Core.IValidationRule<double>
			{
				public bool IsValid(in double value) => value > 0;

				public string GetErrorMessage(in double value) => "Must be positive.";
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(ValidationRuleConventionsAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task GivenRuleMissingMessageFormat_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing;

			public readonly record struct CodeOnlyRule : ZodSharp.Core.IValidationRule<double>
			{
				public const string ErrorCode = "too_small";

				public bool IsValid(in double value) => value > 0;

				public string GetErrorMessage(in double value) => "Must be positive.";
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(ValidationRuleConventionsAnalyzer.DiagnosticId);
		await Assert.That(result).HasDiagnostics(1);
	}

	[Test]
	public async Task GivenRuleWithStaticReadonlyFieldsNotConstants_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing;

			public readonly record struct MutableRule : ZodSharp.Core.IValidationRule<double>
			{
				public static readonly string ErrorCode = "too_small";
				public static readonly string MessageFormat = "Must be positive, but got {0}";

				public bool IsValid(in double value) => value > 0;

				public string GetErrorMessage(in double value) => string.Format(MessageFormat, value);
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(ValidationRuleConventionsAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task GivenRuleWithNonPublicConstants_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing;

			public readonly record struct HiddenRule : ZodSharp.Core.IValidationRule<double>
			{
				const string ErrorCode = "too_small";
				const string MessageFormat = "Must be positive, but got {0}";

				public bool IsValid(in double value) => value > 0;

				public string GetErrorMessage(in double value) => string.Format(MessageFormat, value);
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(ValidationRuleConventionsAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task GivenRuleInheritingConstantsFromBase_HasNoDiagnostics(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing;

			public abstract class RuleBase<T> : ZodSharp.Core.IValidationRule<T>
				where T : IComparable<T>
			{
				public const string ErrorCode = "invalid_value";
				public const string MessageFormat = "Invalid value: {0}";

				public abstract bool IsValid(in T value);

				public string GetErrorMessage(in T value) => string.Format(MessageFormat, value);
			}

			public sealed class PositiveDoubleRule : RuleBase<double>
			{
				public override bool IsValid(in double value) => value > 0;
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task GivenConcreteRuleDerivedFromBareBase_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing;

			public abstract class BareRuleBase<T> : ZodSharp.Core.IValidationRule<T>
			{
				public abstract bool IsValid(in T value);

				public string GetErrorMessage(in T value) => "Invalid.";
			}

			public sealed class DerivedBareRule : BareRuleBase<double>
			{
				public override bool IsValid(in double value) => true;
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		// The abstract base is skipped; only the concrete derivation is reported.
		await Assert.That(result).HasDiagnostic(ValidationRuleConventionsAnalyzer.DiagnosticId);
		await Assert.That(result).HasDiagnostics(1);
	}

	[Test]
	public async Task GivenGenericRuleWithConstants_HasNoDiagnostics(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing;

			public readonly record struct RangeRule<T> : ZodSharp.Core.IValidationRule<T>
				where T : IComparable<T>
			{
				public const string ErrorCode = "invalid_value";
				public const string MessageFormat = "Out of range: {0}";

				public bool IsValid(in T value) => true;

				public string GetErrorMessage(in T value) => string.Format(MessageFormat, value);
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task GivenNonRuleType_HasNoDiagnostics(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing;

			public sealed class NotARule
			{
				public string? Name { get; set; }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
