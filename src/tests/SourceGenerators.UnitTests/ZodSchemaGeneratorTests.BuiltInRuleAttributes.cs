using ZodSharp.SourceGenerators.Infra;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGeneratorTests
{
	/// <summary>
	/// A rule that a consumer would write under the shipped <c>ZodSharp.Rules</c> namespace, used to exercise the
	/// built-in classification without depending on the runtime assembly's actual rule set.
	/// </summary>
	[Test]
	public async Task RuleAttributeGeneration_GivenBuiltInRuleCollidingWithDataAnnotations_GeneratesSuffixedAttribute(
		CancellationToken cancellationToken
	)
	{
		// Arrange - `RequiredRule` would derive `RequiredAttribute`, which System.ComponentModel.DataAnnotations
		// already ships (and which already maps to the equivalent built-in rule).
		const string source = """
			using ZodSharp.Core;

			namespace ZodSharp.Rules
			{
				[ZodRule]
				public readonly record struct RequiredRule(string? Message = null) : IValidationRule<string>
				{
					public bool IsValid(in string value) => value.Length != 0;

					public string GetErrorMessage(in string value) => Message ?? "Value is required.";
				}
			}
			""";

		// Act
		var driverResult = await GenerateAsync(source, cancellationToken);
		var generated = driverResult.GetSource("RequiredZodAttribute");

		// Assert - the DataAnnotations name is left alone and the rule's attribute is emitted under the "Zod"
		// suffix, so its identity and message stay usable.
		await Assert.That(generated).ContainsGeneratedCode("class RequiredZodAttribute");
		await Assert.That(generated).ContainsGeneratedCode("typeof(global::ZodSharp.Rules.RequiredRule)");
		await Assert.That(generated).ContainsGeneratedCode("public string? Message { get; set; } = null;");
		await Assert.That(driverResult.GetSource("RequiredAttribute")).IsNull();
		await Assert.That(driverResult).DoesNotHaveDiagnostic("ZODSGEN043");
		await Assert.That(driverResult).DoesNotHaveDiagnostic("ZODSGEN032");
	}

	[Test]
	public async Task RuleAttributeGeneration_GivenGenericBoundRule_MirrorsTheDoubleBound(
		CancellationToken cancellationToken
	)
	{
		// Arrange - a generic bound rule; the type-parameter bound is surfaced as a double on the attribute.
		const string source = """
			using ZodSharp.Core;

			namespace Testing.Rules
			{
				[ZodRule]
				public readonly record struct ThresholdRule<T>(T threshold) : IValidationRule<T>
					where T : IComparable<T>
				{
					public bool IsValid(in T value) => value.CompareTo(threshold) >= 0;

					public string GetErrorMessage(in T value) => "Too small.";
				}
			}
			""";

		// Act
		var driverResult = await GenerateAsync(source, cancellationToken);
		var generated = driverResult.GetSource("ThresholdAttribute");

		// Assert
		await Assert.That(generated).ContainsGeneratedCode("public double Threshold { get; set; } = default!;");
		await Assert.That(generated).ContainsGeneratedCode("typeof(global::Testing.Rules.ThresholdRule<>)");
		await Assert.That(driverResult).DoesNotHaveDiagnostic("ZODSGEN043");
		await Assert.That(driverResult).DoesNotHaveDiagnostic("ZODSGEN032");
	}

	[Test]
	public async Task RuleAttributeGeneration_GivenBuiltInRuleWithUnrepresentableParameter_ReportsZodsgen043(
		CancellationToken cancellationToken
	)
	{
		// Arrange - a constructor parameter that cannot be represented as an attribute property.
		const string source = """
			using System;
			using ZodSharp.Core;

			namespace ZodSharp.Rules
			{
				[ZodRule]
				public readonly record struct BlobRule(Guid Blob, string? Message = null) : IValidationRule<string>
				{
					public bool IsValid(in string value) => value.Length != 0;

					public string GetErrorMessage(in string value) => Message ?? "Invalid.";
				}
			}
			""";

		// Act
		var driverResult = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(driverResult).HasDiagnostic("ZODSGEN043");
		await Assert.That(driverResult.GetSource("BlobAttribute")).IsNull();
	}

	[Test]
	public async Task RuleAttributeGeneration_GivenCustomRuleWithUnrepresentableParameter_ReportsZodsgen032(
		CancellationToken cancellationToken
	)
	{
		// Arrange - the same shape, but authored by a consumer: it stays an error so the author notices.
		const string source = """
			using System;
			using ZodSharp.Core;

			namespace Testing.Rules
			{
				[ZodRule]
				public readonly record struct BlobRule(Guid Blob, string? Message = null) : IValidationRule<string>
				{
					public bool IsValid(in string value) => value.Length != 0;

					public string GetErrorMessage(in string value) => Message ?? "Invalid.";
				}
			}
			""";

		// Act
		var driverResult = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(driverResult).HasDiagnostic("ZODSGEN032");
		await Assert.That(driverResult).DoesNotHaveDiagnostic("ZODSGEN043");
	}

	/// <summary>
	/// A <c>[ZodSchema]</c> class that uses the validation attributes generated into the shipped
	/// <c>Purview.ZodSharp</c> assembly for the built-in rules.
	/// </summary>
	const string BuiltInAttributeSource = """
		using System;
		using ZodSharp;
		using ZodSharp.Rules;

		namespace Testing
		{
			[ZodSchema]
			public partial class Contact
			{
				[Email]
				public string Email { get; set; } = string.Empty;

				[E164]
				public string Phone { get; set; } = string.Empty;

				[Regex(Pattern = "^[a-z]+$")]
				public string Code { get; set; } = string.Empty;

				[NonSentinel(Message = "Value must not be the default.")]
				public Guid Id { get; set; }
			}
		}
		""";

	[Test]
	public async Task RuleAttributeGeneration_GivenShippedAttributes_ResolvesTheBuiltInRules(
		CancellationToken cancellationToken
	)
	{
		// Act
		var driverResult = await GenerateAsync(BuiltInAttributeSource, cancellationToken);
		var generated = driverResult.GetSource("ContactSchema");

		// Assert - each shipped attribute resolves to its built-in rule. The regex attribute mirrors the
		// string overload (not the Regex one) and the open generic is closed with the member type.
		await Assert.That(generated).ContainsGeneratedCode("new global::ZodSharp.Rules.EmailRule()");
		await Assert.That(generated).ContainsGeneratedCode("new global::ZodSharp.Rules.E164Rule(null!)");
		await Assert.That(generated).ContainsGeneratedCode("new global::ZodSharp.Rules.RegexRule(\"^[a-z]+$\", null!)");
		await Assert
			.That(generated)
			.ContainsGeneratedCode(
				"new global::ZodSharp.Rules.NonSentinelRule<global::System.Guid>(\"Value must not be the default.\")"
			);
		// The built-in rules own their identity, so the generated validation reads it from the rule.
		await Assert.That(generated).Contains("((global::ZodSharp.Core.IZodRule)");
	}

	[Test]
	public async Task RuleAttributeGeneration_GivenShippedAttributes_ReportsTheRuleErrorCode(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var driverResult = await GenerateAsync(
			BuiltInAttributeSource,
			new ZodSourceGeneratorTestOptions().Compile(),
			cancellationToken
		);
		var assembly = await Assert.That(driverResult.CompilationResult.Assembly).IsNotNull();
		var modelType = assembly.GetType("Testing.Contact")!;
		var validate = assembly.GetType("Testing.ContactSchema")!.GetMethod("Validate")!;

		// Act - an invalid email address is rejected by the built-in EmailRule.
		var invalid = Activator.CreateInstance(modelType)!;
		modelType.GetProperty("Email")!.SetValue(invalid, "not-an-email");
		var invalidResult = validate.Invoke(null, [invalid])!;

		// Assert
		await Assert.That((bool)invalidResult.GetType().GetProperty("IsSuccess")!.GetValue(invalidResult)!).IsFalse();
		var errors = (System.Collections.Immutable.ImmutableArray<Core.ValidationError>)
			invalidResult.GetType().GetProperty("Errors")!.GetValue(invalidResult)!;
		await Assert.That(errors.Any(error => error.Code == Rules.EmailRule.ErrorCode)).IsTrue();
	}

	/// <summary>
	/// The acceptance path: a scalar value object annotated with the shipped <c>[NonSentinel]</c> attribute,
	/// adapted through the value-object scalar rule adapter.
	/// </summary>
	const string BuiltInScalarAttributeSource = """
		using System;
		using System.ComponentModel.DataAnnotations;
		using ZodSharp;
		using ZodSharp.Core;
		using ZodSharp.Rules;

		namespace Purview.ValueObjects.Serialization
		{
			[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
			public sealed class ScalarAttribute : Attribute
			{
				public ScalarAttribute(string propertyName = "Value") => PropertyName = propertyName;

				public string PropertyName { get; }
			}
		}

		namespace Purview.ValueObjects
		{
			public interface IScalarValueObject<TSelf, TValue>
				where TSelf : IScalarValueObject<TSelf, TValue>
			{
				TValue Value { get; }
			}

			// Stands in for the adapter the value-object generator emits into a real consumer.
			public readonly record struct ScalarRuleAdapter<TSelf, TValue, TRule>(TRule Rule)
				: IValidationRule<TSelf>
				where TSelf : IScalarValueObject<TSelf, TValue>
				where TRule : IValidationRule<TValue>
			{
				public bool IsValid(in TSelf value) => Rule.IsValid(value.Value);

				public string GetErrorMessage(in TSelf value) => Rule.GetErrorMessage(value.Value);
			}
		}

		namespace Testing
		{
			[Purview.ValueObjects.Serialization.Scalar]
			[NonSentinel(Message = "TenantId must not be the default.")]
			[ZodSchema]
			public partial record struct TenantId : Purview.ValueObjects.IScalarValueObject<TenantId, Guid>
			{
				public Guid Value { get; init; }
			}
		}
		""";

	[Test]
	public async Task RuleAttributeGeneration_GivenShippedNonSentinelOnScalar_AdaptsAndKeepsTheRuleIdentity(
		CancellationToken cancellationToken
	)
	{
		// Act
		var driverResult = await GenerateAsync(BuiltInScalarAttributeSource, cancellationToken);
		var generated = driverResult.GetSource("TenantIdSchema");

		// Assert - the shipped open-generic attribute closes over the scalar's underlying value and the wrapped
		// built-in rule owns the reported identity.
		await Assert
			.That(generated)
			.ContainsGeneratedCode("new global::ZodSharp.Rules.NonSentinelRule<global::System.Guid>(");
		await Assert
			.That(generated)
			.ContainsGeneratedCode(
				"new global::Purview.ValueObjects.ScalarRuleAdapter<global::Testing.TenantId, global::System.Guid, global::ZodSharp.Rules.NonSentinelRule<global::System.Guid>>("
			);
		await Assert
			.That(generated)
			.ContainsGeneratedCode(
				"((global::ZodSharp.Core.IZodRule)tenantIdCustomRuleInner0).Code ?? \"validation_failed\""
			);
	}

	[Test]
	public async Task RuleAttributeGeneration_GivenShippedNonSentinelOnScalar_ReportsEmptyPathAndRuleCode(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var driverResult = await GenerateAsync(
			BuiltInScalarAttributeSource,
			new ZodSourceGeneratorTestOptions().Compile(),
			cancellationToken
		);
		var assembly = await Assert.That(driverResult.CompilationResult.Assembly).IsNotNull();
		var modelType = assembly.GetType("Testing.TenantId")!;
		var validate = assembly.GetType("Testing.TenantIdSchema")!.GetMethod("Validate")!;

		// Act - the default Guid is Guid.Empty, which the built-in rule rejects.
		var invalidResult = validate.Invoke(null, [Activator.CreateInstance(modelType)!])!;

		// Assert - the error is reported at the scalar itself (empty path) with the built-in rule's own code.
		await Assert.That((bool)invalidResult.GetType().GetProperty("IsSuccess")!.GetValue(invalidResult)!).IsFalse();
		var errors = (System.Collections.Immutable.ImmutableArray<Core.ValidationError>)
			invalidResult.GetType().GetProperty("Errors")!.GetValue(invalidResult)!;
		await Assert.That(errors).HasSingleItem();
		await Assert.That(errors[0].Code).IsEqualTo(Rules.NonSentinelRule<Guid>.ErrorCode);
		await Assert.That(errors[0].Path.Length).IsEqualTo(0);
		await Assert.That(errors[0].Message).IsEqualTo("TenantId must not be the default.");
	}

	/// <summary>
	/// A <c>[ZodSchema]</c> class that uses the new numeric rules, a generic bound rule closed with an
	/// <c>int</c>, and a collision rule's suffixed attribute.
	/// </summary>
	const string NewBuiltInAttributeSource = """
		using System;
		using ZodSharp;
		using ZodSharp.Rules;

		namespace Testing
		{
			[ZodSchema]
			public partial class Metrics
			{
				[MinValue(MinValue = 3)]
				public int Count { get; set; }

				[Even]
				public int EvenCount { get; set; }

				[GreaterThanOrEqual(MinValue = 1.5)]
				public double Ratio { get; set; }

				[MinLengthZod(MinLength = 3, Code = "too_short", Message = "Too short.")]
				public string Code { get; set; } = string.Empty;
			}
		}
		""";

	[Test]
	public async Task RuleAttributeGeneration_GivenNewAndCollisionAttributes_ResolvesTheRules(
		CancellationToken cancellationToken
	)
	{
		// Act
		var driverResult = await GenerateAsync(NewBuiltInAttributeSource, cancellationToken);
		var generated = driverResult.GetSource("MetricsSchema");

		// Assert - the generic bound rule is closed with the member type (its double bound converted to int),
		// the generic even rule is closed with int, and the suffixed length attribute carries code and message.
		await Assert.That(generated).ContainsGeneratedCode("new global::ZodSharp.Rules.MinValueRule<int>(3)");
		await Assert.That(generated).ContainsGeneratedCode("new global::ZodSharp.Rules.EvenRule<int>(null!)");
		await Assert
			.That(generated)
			.ContainsGeneratedCode("new global::ZodSharp.Rules.GreaterThanOrEqualRule(1.5D, null!)");
		await Assert
			.That(generated)
			.ContainsGeneratedCode("new global::ZodSharp.Rules.MinLengthRule(3, \"Too short.\", \"too_short\")");
	}

	[Test]
	public async Task RuleAttributeGeneration_GivenNewAndCollisionAttributes_ReportsTheirCodes(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var driverResult = await GenerateAsync(
			NewBuiltInAttributeSource,
			new ZodSourceGeneratorTestOptions().Compile(),
			cancellationToken
		);
		var assembly = await Assert.That(driverResult.CompilationResult.Assembly).IsNotNull();
		var modelType = assembly.GetType("Testing.Metrics")!;
		var validate = assembly.GetType("Testing.MetricsSchema")!.GetMethod("Validate")!;

		// Act - the default metrics are all invalid; the even member is set odd.
		var instance = Activator.CreateInstance(modelType)!;
		modelType.GetProperty("EvenCount")!.SetValue(instance, 3);
		var result = validate.Invoke(null, [instance])!;

		// Assert
		await Assert.That((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).IsFalse();
		var errors = (System.Collections.Immutable.ImmutableArray<Core.ValidationError>)
			result.GetType().GetProperty("Errors")!.GetValue(result)!;
		await Assert.That(errors.Any(error => error.Code == Rules.MinValueRule<int>.ErrorCode)).IsTrue();
		await Assert.That(errors.Any(error => error.Code == Rules.EvenRule<int>.ErrorCode)).IsTrue();
		await Assert.That(errors.Any(error => error.Code == Rules.GreaterThanOrEqualRule.ErrorCode)).IsTrue();
		await Assert.That(errors.Any(error => error.Code == "too_short")).IsTrue();
	}
}
