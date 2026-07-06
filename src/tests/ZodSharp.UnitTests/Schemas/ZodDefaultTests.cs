namespace ZodSharp.Schemas;

public class ZodDefaultTests
{
	[Test]
	public async Task Default_GivenNullInput_ReturnsDefaultValue()
	{
		var result = Z.String().Default("unknown").Validate(null);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("unknown");
	}

	[Test]
	public async Task Default_GivenNonNullInput_ReturnsInputValue()
	{
		var result = Z.String().Default("unknown").Validate("known");

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("known");
	}

	[Test]
	public async Task Default_GivenDefaultShorterThanInnerMinimum_CurrentlyReturnsDefaultWithoutValidation()
	{
		var result = Z.String().Min(3).Default("no").Validate(null);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("no");
	}
}
