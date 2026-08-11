namespace ZodSharp.Schemas;

public class ZodCatchTests
{
	[Test]
	public async Task Catch_GivenValidInput_ReturnsValidatedValue()
	{
		// Arrange
		var schema = Z.Number().Min(0).Catch(0.0);

		// Act
		var result = schema.Validate(5.0);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo(5.0);
	}

	[Test]
	public async Task Catch_GivenInvalidInput_ReturnsFallback()
	{
		// Arrange
		var schema = Z.Number().Min(0).Catch(42.0);

		// Act
		var result = schema.Validate(-1.0);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo(42.0);
	}

	[Test]
	public async Task Catch_GivenFactory_ReturnsComputedFallback()
	{
		// Arrange
		var schema = Z.String()
			.Min(3)
			.Catch(static (value, errors) => $"fallback for '{value}' ({errors.Length} error(s))");

		// Act
		var result = schema.Validate("hi");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("fallback for 'hi' (1 error(s))");
	}

	[Test]
	public async Task Catch_GivenValidInput_DoesNotInvokeFactory()
	{
		// Arrange
		var invoked = false;
		var schema = Z.String().Min(1).Catch(static (v, _) => $"fallback-{v}");

		// Act
		var result = schema.Validate("ok");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("ok");
		await Assert.That(invoked).IsFalse();
	}
}
