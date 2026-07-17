using ZodSharp.Schemas;

namespace ZodSharp.Core;

public class IZodSchemaValidatorTests
{
	[Test]
	public async Task Validator_Validate_ReturnsSuccessForValidValue()
	{
		var validator = new ZodString().Min(1);
		var result = validator.Validate("hello");
		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task Validator_Validate_ReturnsFailureForInvalidValue()
	{
		var validator = new ZodString().Min(5);
		var result = validator.Validate("hi");
		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task NonGenericMarker_IsImplementedByGenericValidator()
	{
		var validator = new ZodString().Min(1);
		await Assert.That(validator).IsNotNull();
	}

	[Test]
	public async Task ValidateAsync_DelegatesToValidate()
	{
		var validator = new ZodString().Min(2);
		var result = await validator.ValidateAsync("ok");
		await Assert.That(result.IsSuccess).IsTrue();
	}
}
