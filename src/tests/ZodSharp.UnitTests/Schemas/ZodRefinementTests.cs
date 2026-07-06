namespace ZodSharp.Schemas;

public class ZodRefinementTests
{
	[Test]
	[Arguments(4.0, true)]
	[Arguments(5.0, false)]
	public async Task RefineEvenNumber_GivenValue_ReturnsExpectedResult(double value, bool expected)
	{
		var result = Z.Number().Refine(static n => n % 2 == 0, "Must be even").Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
		if (!expected)
		{
			await Assert.That(result.Errors[0].Message).IsEqualTo("Must be even");
		}
	}

	[Test]
	[Arguments("Password123", true)]
	[Arguments("password123", false)]
	[Arguments("PASSWORD123", false)]
	[Arguments("Password", false)]
	public async Task PasswordRefinement_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var schema = Z.String()
			.Min(8)
			.Refine(static s => s.Any(char.IsUpper), "Must contain uppercase")
			.Refine(static s => s.Any(char.IsLower), "Must contain lowercase")
			.Refine(static s => s.Any(char.IsDigit), "Must contain digit");

		var result = schema.Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}
}
