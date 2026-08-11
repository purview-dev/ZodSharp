namespace ZodSharp.Schemas;

public class ZodPrefaultTests
{
	[Test]
	public async Task Prefault_GivenDefaultInput_SubstitutesAndValidatesPrefaultValue()
	{
		// Arrange — prefault "tuna" then require min length 3; the prefault value IS validated.
		var schema = Z.String().Min(3).Prefault("tuna");

		// Act
		var result = schema.Validate(null!);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("tuna");
	}

	[Test]
	public async Task Prefault_GivenInvalidPrefaultValue_ReturnsFailure()
	{
		// Arrange — prefault "no" (length 2) but schema requires min 3; prefault is validated.
		var schema = Z.String().Min(3).Prefault("no");

		// Act
		var result = schema.Validate(null!);

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task Prefault_GivenNonNullInput_ValidatesInputAsIs()
	{
		// Arrange
		var schema = Z.String().Min(2).Prefault("default");

		// Act
		var result = schema.Validate("hello");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("hello");
	}

	[Test]
	public async Task Prefault_GivenPrefaultValue_ThenTransform_AppliesTransformToPrefault()
	{
		// Arrange — trim + upper, prefault a value with whitespace to prove it runs the pipeline.
		var schema = Z.String().Trim().Prefault("  hi  ");

		// Act
		var result = schema.Validate(null!);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("hi");
	}
}
