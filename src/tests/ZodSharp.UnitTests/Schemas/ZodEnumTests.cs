namespace ZodSharp.Schemas;

public class ZodEnumTests
{
	public enum Color
	{
		Red,
		Green,
		Blue,
	}

	[Test]
	[Arguments("red", false)]
	[Arguments("Green", true)]
	[Arguments("Blue", true)]
	[Arguments("yellow", false)]
	public async Task StringEnum_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		// Arrange
		var schema = Z.Enum("Red", "Green", "Blue");

		// Act
		var result = schema.Validate(value);

		// Assert
		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	public async Task StringEnum_GivenNull_ReturnsFailure()
	{
		// Arrange
		var schema = Z.Enum("a", "b");

		// Act
		var result = schema.Validate(null!);

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("invalid_type");
	}

	[Test]
	[Arguments(Color.Red, true)]
	[Arguments(Color.Green, true)]
	[Arguments(Color.Blue, true)]
	public async Task NativeEnum_GivenDefinedValue_ReturnsSuccess(Color value, bool expected)
	{
		// Arrange
		var schema = Z.Enum<Color>();

		// Act
		var result = schema.Validate(value);

		// Assert
		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	public async Task NativeEnum_GivenUndefinedValue_ReturnsFailure()
	{
		// Arrange
		var schema = Z.Enum<Color>();

		// Act — (Color)999 is not a defined enum member.
		var result = schema.Validate((Color)999);

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("invalid_enum_value");
	}
}
