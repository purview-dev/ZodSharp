namespace System;

public class StringExtensionsTests
{
	[Test]
	public async Task LengthOrDefault_GivenNullString_Returns0()
	{
		// Arrange
		const string? input = null;

		// Act
		var result = input.LengthOrDefault();

		// Assert
		await Assert.That(result).IsEqualTo(0);
	}

	[Test]
	[Arguments("", 0)]
	[Arguments("one", 3)]
	[Arguments("one two", 7)]
	[Arguments("one two three", 13)]
	[Arguments("one two three ", 14)]
	[Arguments(" one two three", 14)]
	[Arguments(" one two three ", 15)]
	public async Task LengthOrDefault_GivenNonNullStrings_ReturnsCorrectLength(string value, int length)
	{
		// Act
		var result = value.LengthOrDefault();

		// Assert
		await Assert.That(result).IsEqualTo(length);
	}

	[Test]
	[Arguments(null, "actual-null")]
	[Arguments("", "empty-space")]
	[Arguments(" ", "1-whitespace")]
	[Arguments("    ", "some-whitespace")]
	[Arguments("                   ", "more-whitespace")]
	[Arguments("        \n           ", "whitespace-with-newline")]
	[Arguments("\n        \n           \n", "whitespace-with-newlines")]
	public async Task Or_GivenStringIsNullEmptyOrWhitespace_ReturnsSpecifiedDefaultValue(
		string? value,
		string defaultValue
	)
	{
		// Act
		var result = value.Or(defaultValue);

		// Assert
		await Assert.That(result).IsEqualTo(defaultValue);
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments(" ")]
	[Arguments("    ")]
	[Arguments("                   ")]
	[Arguments("        \n           ")]
	[Arguments("\n        \n           \n")]
	public async Task OrNull_GivenStringIsNullEmptyOrWhitespace_ReturnsNull(string? value)
	{
		// Act
		var result = value.OrNull();

		// Assert
		await Assert.That(result).IsNull();
	}

	[Test]
	[Arguments("one")]
	[Arguments("one two")]
	[Arguments("one two three")]
	[Arguments("one two three ")]
	[Arguments(" one two three")]
	[Arguments(" one two three ")]
	public async Task OrNull_GivenNonNullStrings_ReturnsPassedInString(string value)
	{
		// Act
		var result = value.OrNull();

		// Assert
		await Assert.That(result).IsEqualTo(value);
	}
}
