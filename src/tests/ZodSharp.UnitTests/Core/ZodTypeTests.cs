namespace ZodSharp.Core;

public class ZodTypeTests
{
	[Test]
	public async Task OptionalValidate_GivenNull_ReturnsSuccess()
	{
		var result = Z.Optional(Z.String()).Validate(null);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsNull();
	}

	[Test]
	public async Task OptionalValidate_GivenValue_ReturnsSuccess()
	{
		var result = Z.Optional(Z.String()).Validate("value");

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("value");
	}

	[Test]
	public async Task StringValidate_GivenNull_ReturnsFailureWithoutThrowing()
	{
		var result = Z.String().Validate(null!);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("invalid_type");
	}

	[Test]
	public async Task Parse_GivenValidData_ReturnsValue()
	{
		var value = Z.String().Min(3).Parse("John");

		await Assert.That(value).IsEqualTo("John");
	}

	[Test]
	public async Task Parse_GivenInvalidData_ThrowsZodException()
	{
		var exception = Assert.Throws<ZodException>(() => Z.String().Min(3).Parse("AB"));

		await Assert.That(exception).IsNotNull();
		await Assert.That(exception!.Errors).Count().IsEqualTo(1);
	}
}
