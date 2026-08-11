namespace ZodSharp.Schemas;

public class ZodPipeTests
{
	[Test]
	public async Task Pipe_GivenSourceAndTarget_BothValidateInput()
	{
		// Arrange — pipe a string schema into a number-parsing schema via a transform target.
		var source = Z.String().Min(1);
		var target = Z.String().Transform(static s => s.Length);
		var piped = source.Pipe(target);

		// Act
		var result = piped.Validate("hello");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo(5);
	}

	[Test]
	public async Task Pipe_GivenSourceFails_PropagatesSourceErrors()
	{
		// Arrange
		var source = Z.String().Min(10);
		var target = Z.String().Transform(static s => s.Length);
		var piped = source.Pipe(target);

		// Act
		var result = piped.Validate("hi");

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("validation_failed");
	}

	[Test]
	public async Task Pipe_GivenTargetFails_PropagatesTargetErrors()
	{
		// Arrange — source passes, target (min length on transformed value) fails.
		var source = Z.String().Min(1);
		// Target schema requires the (string) value to be >= 5 after passing source.
		var target = Z.String().Min(5);
		var piped = source.Pipe(target);

		// Act
		var result = piped.Validate("hi");

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("validation_failed");
	}

	[Test]
	public async Task Pipe_GivenChainedPipes_ComposesInOrder()
	{
		// Arrange
		var first = Z.String().Trim();
		var second = Z.String().Transform(static s => s.ToUpperInvariant());
		var chained = first.Pipe(second);

		// Act
		var result = chained.Validate("  hello  ");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("HELLO");
	}
}
