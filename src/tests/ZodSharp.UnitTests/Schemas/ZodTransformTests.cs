namespace ZodSharp.Schemas;

public class ZodTransformTests
{
	[Test]
	public async Task TransformString_GivenLowercase_ReturnsUppercase()
	{
		var result = Z.String().Transform(static s => s.ToUpperInvariant()).Validate("hello");

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("HELLO");
	}

	[Test]
	[Arguments(5.0, 10.0)]
	[Arguments(0.0, 0.0)]
	[Arguments(-3.0, -6.0)]
	public async Task TransformNumber_GivenValue_ReturnsDoubledValue(double value, double expected)
	{
		var result = Z.Number().Transform(static n => n * 2).Validate(value);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo(expected);
	}

	[Test]
	public async Task TransformChain_GivenWhitespaceAndUppercase_ReturnsTrimmedLowercase()
	{
		var result = Z.String()
			.Transform(static s => s.Trim())
			.Transform(static s => s.ToLowerInvariant())
			.Validate("  HELLO  ");

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("hello");
	}

	[Test]
	public async Task Transform_GivenTransformThrows_ReturnsFailure()
	{
		var result = Z.String().Transform<string>(_ => throw new InvalidOperationException("boom")).Validate("hello");

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("transform_error");
	}
}
