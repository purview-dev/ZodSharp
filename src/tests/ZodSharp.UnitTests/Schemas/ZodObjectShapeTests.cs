namespace ZodSharp.Schemas;

public class ZodObjectShapeTests
{
	static ZodObject CreateUserSchema() =>
		Z.Object()
			.Field("name", Z.String().Min(1))
			.Field("age", Z.Number().Min(0))
			.Field("email", Z.String().Email())
			.Build();

	[Test]
	public async Task Extend_GivenNewField_ReturnsObjectWithExtraField()
	{
		// Arrange
		var baseObj = Z.Object().Field("name", Z.String()).Build();

		// Act
		var extended = baseObj.Extend("age", Z.Number().Min(0));
		var result = extended.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["age"] = 30.0 }
		);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).ContainsKey("age");
	}

	[Test]
	public async Task Extend_GivenExistingField_ReplacesSchema()
	{
		// Arrange
		var base_ = Z.Object().Field("name", Z.String()).Build();

		// Act — replace name schema with one requiring min length 5.
		var extended = base_.Extend("name", Z.String().Min(5));
		var shortResult = extended.Validate(new Dictionary<string, object?> { ["name"] = "Jo" });

		// Assert
		await Assert.That(shortResult.IsSuccess).IsFalse();
	}

	[Test]
	public async Task Merge_GivenTwoObjects_CombinesShapes()
	{
		// Arrange
		var a = Z.Object().Field("name", Z.String()).Build();
		var b = Z.Object().Field("age", Z.Number()).Build();

		// Act
		var merged = a.Merge(b);
		var result = merged.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["age"] = 30.0 }
		);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).ContainsKey("name");
		await Assert.That(result.Value).ContainsKey("age");
	}

	[Test]
	public async Task Pick_GivenKeys_ReturnsObjectWithOnlyThoseKeys()
	{
		// Arrange
		var schema = CreateUserSchema();

		// Act
		var picked = schema.Pick("name", "email");
		var resultWithAll = picked.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["email"] = "john@example.com" }
		);
		var resultMissingAge = picked.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["email"] = "john@example.com" }
		);

		// Assert — age is not in the picked shape, so it's not required.
		await Assert.That(resultWithAll.IsSuccess).IsTrue();
		await Assert.That(resultMissingAge.IsSuccess).IsTrue();
	}

	[Test]
	public async Task Omit_GivenKeys_ReturnsObjectWithoutThoseKeys()
	{
		// Arrange
		var schema = CreateUserSchema();

		// Act
		var omitted = schema.Omit("age");
		var result = omitted.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["email"] = "john@example.com" }
		);

		// Assert — age is omitted, so it's not required.
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value!).ContainsKey("name");
		await Assert.That(result.Value!.ContainsKey("age")).IsFalse();
	}

	[Test]
	public async Task Partial_GivenMissingFields_AllowsMissing()
	{
		// Arrange
		var schema = CreateUserSchema().Partial();

		// Act — only name provided; age and email are missing but optional.
		var result = schema.Validate(new Dictionary<string, object?> { ["name"] = "John" });

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task Partial_GivenPresentFields_StillValidatesThem()
	{
		// Arrange
		var schema = CreateUserSchema().Partial();

		// Act — age is present but invalid (negative).
		var result = schema.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["age"] = -5.0 }
		);

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task Required_GivenPartialThenRequired_EnforcesAllFields()
	{
		// Arrange
		var partial = CreateUserSchema().Partial();
		var required = partial.Required();

		// Act — only name provided; age and email are required again.
		var result = required.Validate(new Dictionary<string, object?> { ["name"] = "John" });

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task Strict_GivenUnknownKey_ReturnsError()
	{
		// Arrange
		var schema = Z.Object().Field("name", Z.String()).Build().Strict();

		// Act
		var result = schema.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["extra"] = "unknown" }
		);

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors.Any(static e => e.Code == "unrecognized_key")).IsTrue();
	}

	[Test]
	public async Task Passthrough_GivenUnknownKey_KeepsItInOutput()
	{
		// Arrange
		var schema = Z.Object().Field("name", Z.String()).Build().Passthrough();

		// Act
		var result = schema.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["extra"] = "kept" }
		);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value!["extra"]).IsEqualTo("kept");
	}

	[Test]
	public async Task Strip_GivenUnknownKey_DropsItFromOutput()
	{
		// Arrange
		var schema = Z.Object().Field("name", Z.String()).Build().Strip();

		// Act
		var result = schema.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["extra"] = "dropped" }
		);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value!.ContainsKey("extra")).IsFalse();
	}

	[Test]
	public async Task Catchall_GivenUnknownKey_ValidatesAndIncludesIt()
	{
		// Arrange — catchall requires strings; an int extra should fail.
		var schema = Z.Object()
			.Field("name", Z.String())
			.Build()
			.Catchall(new FieldSchemaWrapper<string>(Z.String()));

		// Act
		var validExtra = schema.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["extra"] = "ok" }
		);
		var invalidExtra = schema.Validate(
			new Dictionary<string, object?> { ["name"] = "John", ["extra"] = 42 }
		);

		// Assert
		await Assert.That(validExtra.IsSuccess).IsTrue();
		await Assert.That(validExtra.Value!["extra"]).IsEqualTo("ok");
		await Assert.That(invalidExtra.IsSuccess).IsFalse();
	}
}
