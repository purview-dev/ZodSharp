namespace ZodSharp.Schemas;

public class ZodObjectTests
{
	[Test]
	public async Task ObjectValidate_GivenCompleteObject_ReturnsSuccess()
	{
		var schema = CreateUserSchema();
		var data = new Dictionary<string, object?>
		{
			["name"] = "John Doe",
			["age"] = 30.0,
			["email"] = "john@example.com",
		};

		var result = schema.Validate(data);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value!["name"]).IsEqualTo("John Doe");
	}

	[Test]
	public async Task ObjectValidate_GivenMissingRequiredField_ReturnsFailure()
	{
		var schema = CreateUserSchema();
		var data = new Dictionary<string, object?> { ["name"] = "John Doe", ["age"] = 30.0 };

		var result = schema.Validate(data);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors.Any(error => error.Code == "missing_field")).IsTrue();
	}

	[Test]
	public async Task ObjectValidate_GivenWrongFieldType_ReturnsFailure()
	{
		var schema = CreateUserSchema();
		var data = new Dictionary<string, object?>
		{
			["name"] = "John Doe",
			["age"] = "not-a-number",
			["email"] = "john@example.com",
		};

		var result = schema.Validate(data);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Path).Contains("age");
	}

	[Test]
	public async Task ObjectValidate_GivenInvalidEmail_ReturnsFailure()
	{
		var schema = CreateUserSchema();
		var data = new Dictionary<string, object?>
		{
			["name"] = "John Doe",
			["age"] = 30.0,
			["email"] = "not-an-email",
		};

		var result = schema.Validate(data);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Path).Contains("email");
	}

	static ZodSharp.Schemas.ZodObject CreateUserSchema() =>
		Z.Object()
			.Field("name", Z.String().Min(1))
			.Field("age", Z.Number().Min(0).Max(120))
			.Field("email", Z.String().Email())
			.Build();
}
