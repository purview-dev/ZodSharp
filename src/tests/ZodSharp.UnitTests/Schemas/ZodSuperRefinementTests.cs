namespace ZodSharp.Schemas;

public class ZodSuperRefinementTests
{
	[Test]
	public async Task SuperRefine_GivenPassingPredicate_ReturnsSuccess()
	{
		// Arrange
		var schema = Z.Number()
			.SuperRefine(static ctx =>
			{
				if (ctx.Value % 2 != 0)
					ctx.AddIssue("Must be even");
			});

		// Act
		var result = schema.Validate(4.0);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo(4.0);
	}

	[Test]
	public async Task SuperRefine_GivenFailingPredicate_ReturnsFailureWithMessage()
	{
		// Arrange
		var schema = Z.Number()
			.SuperRefine(static ctx =>
			{
				if (ctx.Value % 2 != 0)
					ctx.AddIssue("Must be even");
			});

		// Act
		var result = schema.Validate(5.0);

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Message).IsEqualTo("Must be even");
	}

	[Test]
	public async Task SuperRefine_GivenMultipleIssues_ReturnsAllIssues()
	{
		// Arrange
		var schema = Z.String()
			.Min(8)
			.SuperRefine(static ctx =>
			{
				if (!ctx.Value.Any(char.IsUpper))
					ctx.AddIssue("must_contain_upper", "Must contain an uppercase letter");
				if (!ctx.Value.Any(char.IsDigit))
					ctx.AddIssue("must_contain_digit", "Must contain a digit");
			});

		// Act
		var result = schema.Validate("password");

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors.Count).IsEqualTo(2);
		await Assert.That(result.Errors[0].Code).IsEqualTo("must_contain_upper");
		await Assert.That(result.Errors[1].Code).IsEqualTo("must_contain_digit");
	}

	[Test]
	public async Task SuperRefine_GivenBaseSchemaFails_PropagatesBaseErrorsOnly()
	{
		// Arrange
		var schema = Z.String()
			.Min(5)
			.SuperRefine(static ctx =>
			{
				if (ctx.Value.Length > 100)
					ctx.AddIssue("too_long");
			});

		// Act
		var result = schema.Validate("hi");

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors.Count).IsEqualTo(1);
		await Assert.That(result.Errors[0].Code).IsEqualTo("validation_failed");
	}

	[Test]
	public async Task SuperRefine_GivenIssueWithPath_ReturnsPathedIssue()
	{
		// Arrange
		var schema = Z.String().SuperRefine(static ctx => ctx.AddIssue("bad", "no good", ["nested"]));

		// Act
		var result = schema.Validate("anything");

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Path).Contains("nested");
	}
}
