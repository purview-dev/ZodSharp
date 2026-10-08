using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators;

public partial class ZodSchemaAnalyzerTests
{
	/// <summary>
	/// Stand-in Purview.ValueObjects scalar attributes (both automatic forms, with <c>Nullable</c> on the
	/// shared base), because this repository does not reference the value-objects package.
	/// </summary>
	const string AutomaticScalarAnalyzerPrelude = """
		using System;

		namespace Purview.ValueObjects.Serialization
		{
			public abstract class ScalarOptionsAttribute : Attribute
			{
				protected ScalarOptionsAttribute(string propertyName) => PropertyName = propertyName;

				public string PropertyName { get; }

				public bool Nullable { get; init; }
			}

			[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
			public sealed class ScalarAttribute : ScalarOptionsAttribute
			{
				public ScalarAttribute(string propertyName = "Value") : base(propertyName) { }

				public ScalarAttribute(Type valueType, string propertyName = "Value") : base(propertyName) =>
					ValueType = valueType;

				public Type? ValueType { get; }
			}

			[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
			public sealed class ScalarAttribute<TValue>(string propertyName = "Value")
				: ScalarOptionsAttribute(propertyName);
		}
		""";

	[Test]
	public async Task AutomaticScalar_GivenSuppressedValidateMethod_ProducesZODSGEN044(
		CancellationToken cancellationToken
	)
	{
		// Arrange — the value-object generator's Create always calls {Type}Schema.Validate, so suppressing
		// the Validate method leaves a dangling reference.
		const string source =
			AutomaticScalarAnalyzerPrelude
			+ """
				namespace Testing
				{
					[Purview.ValueObjects.Serialization.Scalar<System.Guid>]
					[ZodSchema(GenerateValidateMethod = false)]
					public readonly partial record struct InstallationId { }
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AutomaticScalarSchemaUnavailable);
	}

	[Test]
	public async Task AutomaticScalar_GivenTypeofFormWithSuppressedValidateMethod_ProducesZODSGEN044(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			AutomaticScalarAnalyzerPrelude
			+ """
				namespace Testing
				{
					[Purview.ValueObjects.Serialization.Scalar(typeof(System.Guid))]
					[ZodSchema(GenerateValidateMethod = false)]
					public readonly partial record struct InstallationId { }
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AutomaticScalarSchemaUnavailable);
	}

	[Test]
	public async Task AutomaticScalar_GivenUnrepresentableUnderlyingType_ProducesZODSGEN044(
		CancellationToken cancellationToken
	)
	{
		// Arrange — the underlying type is unresolved, so no schema property can be generated for it.
		const string source =
			AutomaticScalarAnalyzerPrelude
			+ """
				namespace Testing
				{
					[Purview.ValueObjects.Serialization.Scalar(typeof(MissingValueType))]
					[ZodSchema]
					public readonly partial record struct Weird { }
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AutomaticScalarSchemaUnavailable);
	}

	[Test]
	public async Task AutomaticScalar_GivenValidAutomaticForm_ProducesNoZODSGEN044(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			AutomaticScalarAnalyzerPrelude
			+ """
				namespace Testing
				{
					[Purview.ValueObjects.Serialization.Scalar<System.Guid>]
					[ZodSchema]
					public readonly partial record struct InstallationId { }
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.AutomaticScalarSchemaUnavailable);
	}

	[Test]
	public async Task AutomaticScalar_GivenManualFormWithSuppressedValidateMethod_ProducesNoZODSGEN044(
		CancellationToken cancellationToken
	)
	{
		// Arrange — the diagnostic is scoped to the automatic forms.
		const string source =
			AutomaticScalarAnalyzerPrelude
			+ """
				namespace Testing
				{
					[Purview.ValueObjects.Serialization.Scalar("Value")]
					[ZodSchema(GenerateValidateMethod = false)]
					public readonly partial record struct Email
					{
						public string Value { get; init; } = string.Empty;
					}
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.AutomaticScalarSchemaUnavailable);
	}
}
