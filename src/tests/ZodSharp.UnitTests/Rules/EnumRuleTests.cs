using System.Globalization;
using System.Text;
using ZodSharp.Schemas;

namespace ZodSharp.Rules;

public class EnumRuleTests
{
	enum Color
	{
		Red,

		Green,

		Blue,
	}

	[Test]
	public async Task IsValid_GivenDefinedMember_ReturnsTrue()
	{
		// Arrange
		EnumRule<Color> rule = new();

		// Act
		var isValid = rule.IsValid(Color.Green);

		// Assert
		await Assert.That(isValid).IsTrue();
	}

	[Test]
	public async Task IsValid_GivenUndefinedMember_ReturnsFalse()
	{
		// Arrange
		EnumRule<Color> rule = new();

		// Act
		var isValid = rule.IsValid((Color)999);

		// Assert
		await Assert.That(isValid).IsFalse();
	}

	[Test]
	public async Task IsValid_GivenDisallowedMember_ReturnsFalse()
	{
		// Arrange
		EnumRule<Color> rule = new([Color.Blue]);

		// Act
		var isValid = rule.IsValid(Color.Blue);

		// Assert
		await Assert.That(isValid).IsFalse();
	}

	[Test]
	public async Task IsValid_GivenOtherMemberWithDisallowedSet_ReturnsTrue()
	{
		// Arrange
		EnumRule<Color> rule = new([Color.Blue]);

		// Act
		var isValid = rule.IsValid(Color.Red);

		// Assert
		await Assert.That(isValid).IsTrue();
	}

	[Test]
	public async Task IsValid_GivenEmptyDisallowedSet_AcceptsEveryDefinedMember()
	{
		// Arrange
		EnumRule<Color> rule = new([]);

		// Act
		var isValid = rule.IsValid(Color.Blue);

		// Assert
		await Assert.That(isValid).IsTrue();
	}

	[Test]
	public async Task IsValid_GivenDisallowedUndefinedMember_RejectsIt()
	{
		// Arrange - a disallowed value is rejected whether or not it is a defined member.
		EnumRule<Color> rule = new([(Color)999]);

		// Act
		var isValid = rule.IsValid((Color)999);

		// Assert
		await Assert.That(isValid).IsFalse();
	}

	[Test]
	public async Task GetErrorMessage_GivenValue_FormatsTheValueAndEnumName()
	{
		// Arrange
		EnumRule<Color> rule = new();

		// Act
		var message = rule.GetErrorMessage((Color)999);
		var expected = string.Format(
			CultureInfo.CurrentCulture,
			CompositeFormat.Parse(EnumRule<Color>.MessageFormat),
			(Color)999,
			"Color"
		);

		// Assert
		await Assert.That(message).IsEqualTo(expected);
	}

	[Test]
	public async Task Code_Always_ReportsInvalidEnumValue()
	{
		// Arrange
		EnumRule<Color> rule = new();

		// Act
		var code = rule.Code;

		// Assert
		await Assert.That(code).IsEqualTo(EnumRule<Color>.ErrorCode);
		await Assert.That(EnumRule<Color>.ErrorCode).IsEqualTo("invalid_enum_value");
	}

	[Test]
	public async Task GetErrorMessage_GivenCustomMessage_UsesIt()
	{
		// Arrange
		EnumRule<Color> rule = new(message: "Custom message.");

		// Act
		var message = rule.GetErrorMessage((Color)999);

		// Assert
		await Assert.That(message).IsEqualTo("Custom message.");
	}

	[Test]
	public async Task Code_GivenOverride_ReportsIt()
	{
		// Arrange
		EnumRule<Color> rule = new(code: "custom_code");

		// Act
		var code = rule.Code;
		var interfaceCode = ((Core.IZodRule)rule).Code;

		// Assert
		await Assert.That(code).IsEqualTo("custom_code");
		await Assert.That(interfaceCode).IsEqualTo("custom_code");
	}

	[Test]
	public async Task Enum_GivenFluentNativeEnumSchema_KeepsTheConcreteSchema()
	{
		// Arrange
		var schema = Z.Enum<Color>();

		// Act
		var chained = schema.Enum();
		var result = chained.Validate(Color.Red);

		// Assert - the extension returns the schema, so the fluent chain keeps its concrete type.
		await Assert.That(chained).IsTypeOf<ZodNativeEnum<Color>>();
		await Assert.That(result.IsSuccess).IsTrue();
	}
}
