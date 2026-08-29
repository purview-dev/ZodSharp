using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators;

public partial class ZodSchemaAnalyzerTests
{
	[Test]
	public async Task DataAnnotations_GivenInvalidLengthAttributeConfiguration_ProducesZODSGEN005(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.ComponentModel.DataAnnotations;

			namespace Testing
			{
				[ZodSchema]
				public sealed class InvalidLengthModel
				{
					[Length(5, 2)]
					public string? Name { get; set; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.InvalidLengthAttribute);
	}

	[Test]
	public async Task DataAnnotations_GivenUnsupportedLengthTarget_ProducesZODSGEN006(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.ComponentModel.DataAnnotations;

			namespace Testing
			{
				[ZodSchema]
				public sealed class UnsupportedLengthModel
				{
					[Length(1, 2)]
					public decimal Amount { get; set; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.UnsupportedLengthAttributeTarget);
	}

	[Test]
	public async Task DataAnnotations_GivenUnsupportedAdditionalDataAnnotations_ProducesZODSGEN006(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System;
			using System.ComponentModel.DataAnnotations;

			namespace Testing
			{
				[ZodSchema]
				public sealed class UnsupportedAnnotationsModel
				{
					[RegularExpression("^[A-Z]{2}$")]
					public int CountryCode { get; set; }

					[AllowedValues(1, 2)]
					public DateTime Timestamp { get; set; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostics("ZODSGEN006", 2);
	}

	[Test]
	public async Task DataAnnotations_GivenInvalidResourceConfiguration_ProducesZODSGEN004(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.ComponentModel.DataAnnotations;

			namespace Testing
			{
				[ZodSchema]
				public sealed class InvalidResourceModel
				{
					[StringLength(5, ErrorMessageResourceName = "OnlyNameProvided")]
					public string Value { get; set; } = string.Empty;
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.InvalidDataAnnotationsErrorMessage);
	}

	[Test]
	public async Task DataAnnotations_GivenStringOnlyAttributesOnNonStringTargets_ProducesZODSGEN006(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.ComponentModel.DataAnnotations;

			namespace Testing
			{
				[ZodSchema]
				public sealed class UnsupportedStringFormatModel
				{
					[Url]
					public int Website { get; set; }

					[Phone]
					public int PhoneNumber { get; set; }

					[CreditCard]
					public int CardNumber { get; set; }

					[Base64String]
					public int Encoded { get; set; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostics("ZODSGEN006", 4);
	}

	[Test]
	public async Task DataAnnotations_GivenCompareAttributeWithMissingProperty_ProducesZODSGEN020(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.ComponentModel.DataAnnotations;

			namespace Testing
			{
				[ZodSchema]
				public sealed class CompareMissingModel
				{
					[Compare("MissingProperty")]
					public string? Password { get; set; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ComparePropertyNotFound);
	}
}
