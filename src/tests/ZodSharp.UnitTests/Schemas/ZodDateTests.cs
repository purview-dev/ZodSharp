namespace ZodSharp.Schemas;

public class ZodDateTests
{
	[Test]
	public async Task DateValidate_GivenValue_ReturnsSuccess()
	{
		var result = Z.Date().Validate(new DateTime(2020, 1, 1));

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo(new DateTime(2020, 1, 1));
	}

	[Test]
	public async Task DateMin_GivenEarlierValue_ReturnsFailure()
	{
		var result = Z.Date().Min(new DateTime(2020, 1, 1)).Validate(new DateTime(2019, 12, 31));

		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task DateMin_GivenEqualValue_ReturnsSuccess()
	{
		var result = Z.Date().Min(new DateTime(2020, 1, 1)).Validate(new DateTime(2020, 1, 1));

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task DateMax_GivenLaterValue_ReturnsFailure()
	{
		var result = Z.Date().Max(new DateTime(2020, 1, 1)).Validate(new DateTime(2020, 1, 2));

		await Assert.That(result.IsSuccess).IsFalse();
	}
}
