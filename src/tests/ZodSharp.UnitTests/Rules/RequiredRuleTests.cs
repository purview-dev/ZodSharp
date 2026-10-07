namespace ZodSharp.Rules;

public class RequiredRuleTests
{
	[Test]
	public async Task IsValid_GivenNullString_ReturnsFalse(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		RequiredRule<string> rule = new();

		// Act
		var isValid = rule.IsValid(null!);

		// Assert
		await Assert.That(isValid).IsFalse();
	}

	[Test]
	public async Task IsValid_GivenEmptyStringAndEmptyNotAllowed_ReturnsFalse(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		RequiredRule<string> rule = new();

		// Act
		var isValid = rule.IsValid(string.Empty);

		// Assert
		await Assert.That(isValid).IsFalse();
	}

	[Test]
	public async Task IsValid_GivenEmptyStringAndEmptyAllowed_ReturnsTrue(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		RequiredRule<string> rule = new(allowEmptyStrings: true);

		// Act
		var isValid = rule.IsValid(string.Empty);

		// Assert
		await Assert.That(isValid).IsTrue();
	}

	[Test]
	public async Task IsValid_GivenWhitespaceAndTrimDisabled_ReturnsTrue(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange - an empty string is rejected, but "   " has a non-zero length so it passes.
		RequiredRule<string> rule = new();

		// Act
		var isValid = rule.IsValid("   ");

		// Assert
		await Assert.That(isValid).IsTrue();
	}

	[Test]
	public async Task IsValid_GivenWhitespaceAndTrimEnabled_ReturnsFalse(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		RequiredRule<string> rule = new(trimWhitespace: true);

		// Act
		var isValid = rule.IsValid("   ");

		// Assert
		await Assert.That(isValid).IsFalse();
	}

	[Test]
	public async Task IsValid_GivenPaddedValueAndTrimEnabled_ReturnsTrue(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange - trimming only affects the empty check; a padded non-empty value stays valid.
		RequiredRule<string> rule = new(trimWhitespace: true);

		// Act
		var isValid = rule.IsValid("  value  ");

		// Assert
		await Assert.That(isValid).IsTrue();
	}

	[Test]
	public async Task IsValid_GivenNullableValueTypeWithValue_ReturnsTrue(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		RequiredRule<int?> rule = new();

		// Act
		var isValid = rule.IsValid(5);

		// Assert
		await Assert.That(isValid).IsTrue();
	}

	[Test]
	public async Task IsValid_GivenNullableValueTypeWithoutValue_ReturnsFalse(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		RequiredRule<int?> rule = new();

		// Act
		var isValid = rule.IsValid(null);

		// Assert
		await Assert.That(isValid).IsFalse();
	}

	[Test]
	public async Task Code_GivenOverride_ReportsIt(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		RequiredRule<string> rule = new(code: "custom_code");

		// Act
		var code = rule.Code;

		// Assert
		await Assert.That(code).IsEqualTo("custom_code");
	}

	[Test]
	public async Task Required_GivenEmptyString_FailsThroughTheFluentAPI(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		var schema = Z.String().Required();

		// Act
		var result = schema.Validate(string.Empty);

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo(RequiredRule<string>.ErrorCode);
	}

	[Test]
	public async Task Required_GivenWhitespaceAndTrimEnabled_FailsThroughTheFluentAPI(
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		var schema = Z.String().Required(trimWhitespace: true);

		// Act
		var result = schema.Validate("   ");

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
	}
}
