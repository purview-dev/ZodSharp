namespace ZodSharp.Schemas;

public class ZodTypedUnionTests
{
	[Test]
	public async Task TypedUnion_GivenValueMatchingFirstOption_ReturnsFirstCase()
	{
		// Arrange
		var schema = Z.Union(Z.String().Min(1), Z.Number().Min(0));

		// Act
		var result = schema.Validate("hello");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value.TryGetValue(out string? s)).IsTrue();
		await Assert.That(s).IsEqualTo("hello");
	}

	[Test]
	public async Task TypedUnion_GivenValueMatchingSecondOption_ReturnsSecondCase()
	{
		// Arrange
		var schema = Z.Union(Z.String().Min(1), Z.Number().Min(0));

		// Act
		var result = schema.Validate(42.0);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value.TryGetValue(out double n)).IsTrue();
		await Assert.That(n).IsEqualTo(42.0);
	}

	[Test]
	public async Task TypedUnion_GivenValueMatchingNeither_ReturnsFailure()
	{
		// Arrange
		var schema = Z.Union(Z.String().Min(10), Z.Number().Min(100));

		// Act — "hi" is too short for string, 5.0 is too small for number.
		var result = schema.Validate("hi");

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("invalid_union");
	}

	[Test]
	public async Task Or_GivenTwoSchemas_ReturnsTypedUnion()
	{
		// Arrange — instance .Or returns a typed union schema.
		var schema = Z.String().Min(10).Or(Z.Number().Positive());

		// Act — short string fails first, 5.0 passes second.
		var result = schema.Validate(5.0);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value.TryGetValue(out double n)).IsTrue();
		await Assert.That(n).IsEqualTo(5.0);
	}

	[Test]
	public async Task Or_GivenValuePassingFirst_ReturnsFirstCase()
	{
		// Arrange
		var schema = Z.String().Min(3).Or(Z.Number());

		// Act
		var result = schema.Validate("hello");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value.TryGetValue(out string? s)).IsTrue();
		await Assert.That(s).IsEqualTo("hello");
	}

	[Test]
	public async Task TypedUnion_GivenMatchOnResult_RunsCorrectCase()
	{
		// Arrange
		var schema = Z.Union(Z.String(), Z.Number());

		// Act
		var result = schema.Validate(42.0);
		var matched = result.Value.Match(static s => $"str:{s}", static n => $"num:{n}");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(matched).IsEqualTo("num:42");
	}
}
