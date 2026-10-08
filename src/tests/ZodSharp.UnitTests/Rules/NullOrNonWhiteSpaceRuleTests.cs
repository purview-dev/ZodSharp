using System.Globalization;
using System.Text;

namespace ZodSharp.Rules;

public class NullOrNonWhiteSpaceRuleTests
{
	[Test]
	[Arguments(null)]
	[Arguments("value")]
	public async Task IsValid_GivenNullOrNonWhitespace_ReturnsTrue(string? value)
	{
		// Arrange
		NullOrNonWhiteSpaceRule rule = new();

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsTrue();
	}

	[Test]
	[Arguments("")]
	[Arguments("   ")]
	public async Task IsValid_GivenEmptyOrWhitespace_ReturnsFalse(string value)
	{
		// Arrange
		NullOrNonWhiteSpaceRule rule = new();

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsFalse();
	}

	[Test]
	public async Task GetErrorMessage_GivenNoCustomMessage_FormatsDefaultMessage()
	{
		// Arrange
		NullOrNonWhiteSpaceRule rule = new();

		// Act
		var message = rule.GetErrorMessage("   ");
		var expected = string.Format(
			CultureInfo.CurrentCulture,
			CompositeFormat.Parse(NullOrNonWhiteSpaceRule.MessageFormat),
			"   "
		);

		// Assert
		await Assert.That(message).IsEqualTo(expected);
	}

	[Test]
	public async Task GetErrorMessage_GivenCustomMessage_ReturnsCustomMessage()
	{
		// Arrange
		NullOrNonWhiteSpaceRule rule = new("Nickname must be null or non-whitespace.");

		// Act
		var message = rule.GetErrorMessage("   ");

		// Assert
		await Assert.That(message).IsEqualTo("Nickname must be null or non-whitespace.");
	}

	[Test]
	public async Task Code_GivenNoOverride_ReturnsErrorCode()
	{
		// Arrange
		NullOrNonWhiteSpaceRule rule = new();

		// Act
		var code = rule.Code;

		// Assert
		await Assert.That(code).IsEqualTo(NullOrNonWhiteSpaceRule.ErrorCode);
	}

	[Test]
	public async Task Code_GivenOverride_ReturnsOverride()
	{
		// Arrange
		NullOrNonWhiteSpaceRule rule = new(code: "custom_code");

		// Act
		var code = rule.Code;

		// Assert
		await Assert.That(code).IsEqualTo("custom_code");
	}
}
