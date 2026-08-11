using ZodSharp.Core;

namespace ZodSharp.Schemas;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Style",
	"IDE0004:Remove Unnecessary Cast",
	Justification = "Explicit testing"
)]
public class SchemaValidatorCompositionTests
{
	[Test]
	public async Task SchemaValidator_GivenZodType_IsAlsoIZodSchema()
	{
		// Arrange — ZodType<T> implements both IZodSchemaValidator<T> and IZodSchema<T>.
		IZodSchemaValidator<string> validator = Z.String().Min(3);

		// Act & Assert — the validator is also an IZodSchema<string>, enabling composition.
		await Assert.That(validator is IZodSchema<string>).IsTrue();
	}

	[Test]
	public async Task SchemaValidator_GivenUsedAsIZodSchema_CanBePassedToAnd()
	{
		// Arrange — a validator (as IZodSchema<string>) can be passed as the 'other'
		// argument to .And(), enabling generated schemas to participate in composition.
		var left = Z.String().Min(1);
		IZodSchemaValidator<string> right = Z.String().Max(10);
		var composed = left.And((IZodSchema<string>)right);

		// Act
		var result = composed.Validate("hello");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task SchemaValidator_GivenUsedAsIZodSchema_CanBePassedToIntersection()
	{
		// Arrange — a validator can be used in Z.Intersection.
		IZodSchemaValidator<string> left = Z.String().Min(3);
		IZodSchemaValidator<string> right = Z.String().Max(10);
		var composed = Z.Intersection((IZodSchema<string>)left, (IZodSchema<string>)right);

		// Act
		var result = composed.Validate("hello");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task SchemaValidator_GivenUsedAsIZodSchema_CanBePassedToOr()
	{
		// Arrange — a validator can be used in .Or() to create a typed union.
		var left = Z.String().Min(10);
		IZodSchemaValidator<double> right = Z.Number().Min(0);
		var composed = left.Or((IZodSchema<double>)right);

		// Act — short string fails left, 5.0 passes right.
		var result = composed.Validate(5.0);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value.TryGetValue(out double n)).IsTrue();
		await Assert.That(n).IsEqualTo(5.0);
	}
}
