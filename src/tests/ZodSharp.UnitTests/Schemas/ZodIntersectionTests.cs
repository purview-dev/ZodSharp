namespace ZodSharp.Schemas;

public class ZodIntersectionTests
{
	[Test]
	public async Task Intersection_GivenValuePassingBoth_ReturnsSuccess()
	{
		// Arrange — both schemas accept the value.
		var left = Z.String().Min(3);
		var right = Z.String().Max(10);
		var intersection = Z.Intersection(left, right);

		// Act
		var result = intersection.Validate("hello");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("hello");
	}

	[Test]
	public async Task Intersection_GivenValueFailingLeft_ReturnsFailureWithLeftErrors()
	{
		// Arrange
		var left = Z.String().Min(10);
		var right = Z.String().Max(100);
		var intersection = Z.Intersection(left, right);

		// Act
		var result = intersection.Validate("hi");

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors.Length).IsGreaterThan(0);
	}

	[Test]
	public async Task Intersection_GivenValueFailingBoth_MergesAllErrors()
	{
		// Arrange
		var left = Z.String().Min(10);
		var right = Z.String().Max(2);
		var intersection = Z.Intersection(left, right);

		// Act
		var result = intersection.Validate("hello");

		// Assert — both too short for left (min 10) and too long for right (max 2).
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors.Length).IsEqualTo(2);
	}

	[Test]
	public async Task And_GivenTwoSchemas_BothMustPass()
	{
		// Arrange — instance .And composes two schemas.
		var schema = Z.Number().Min(0).And(Z.Number().Max(100));

		// Act
		var valid = schema.Validate(50.0);
		var tooBig = schema.Validate(200.0);

		// Assert
		await Assert.That(valid.IsSuccess).IsTrue();
		await Assert.That(tooBig.IsSuccess).IsFalse();
	}

	[Test]
	public async Task And_GivenChainedAnd_AllSchemasMustPass()
	{
		// Arrange — chain .And multiple times.
		var schema = Z.String().Min(3).And(Z.String().Max(10)).And(Z.String().Regex(@"^[a-z]+$"));

		// Act
		var valid = schema.Validate("hello");
		var invalid = schema.Validate("Hello1");

		// Assert
		await Assert.That(valid.IsSuccess).IsTrue();
		await Assert.That(invalid.IsSuccess).IsFalse();
	}
}
