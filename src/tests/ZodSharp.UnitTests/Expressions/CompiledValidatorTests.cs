using ZodSharp.Core;
using ZodSharp.Expressions;

namespace ZodSharp.Expressions;

/// <summary>
/// Pins the caching contract of <see cref="CompiledValidator"/>.
/// </summary>
/// <remarks>
/// The original implementation called <c>LambdaExpression.Compile</c> on every invocation, emitting a
/// <c>DynamicMethod</c> that is never reclaimed. These tests assert the delegate is now cached per
/// schema instance, so the documented inline usage cannot leak.
/// </remarks>
public class CompiledValidatorTests
{
	[Test]
	public async Task Compile_GivenSameSchemaInstance_ReturnsTheSameDelegate()
	{
		// Arrange
		var schema = Z.String().Min(3);

		// Act
		var first = CompiledValidator.Compile(schema);
		var second = CompiledValidator.Compile(schema);

		// Assert — reference equality is the point: a fresh delegate per call is what leaked.
		await Assert.That(ReferenceEquals(first, second)).IsTrue();
	}

	[Test]
	public async Task Compile_GivenDifferentSchemaInstances_ReturnsDifferentDelegates()
	{
		// Arrange
		var first = Z.String().Min(3);
		var second = Z.String().Min(3);

		// Act / Assert
		await Assert
			.That(ReferenceEquals(CompiledValidator.Compile(first), CompiledValidator.Compile(second)))
			.IsFalse();
	}

	[Test]
	public async Task Compile_RepeatedManyTimes_DoesNotGrowUnbounded()
	{
		// Arrange — the previous implementation emitted a DynamicMethod per iteration.
		var schema = Z.String().Min(3);
		var expected = CompiledValidator.Compile(schema);

		// Act
		for (var i = 0; i < 1_000; i++)
			_ = CompiledValidator.Compile(schema);

		// Assert
		await Assert.That(ReferenceEquals(CompiledValidator.Compile(schema), expected)).IsTrue();
	}

	[Test]
	public async Task Compile_ProducesDelegateEquivalentToValidate()
	{
		// Arrange
		var schema = Z.String().Min(3);
		var validator = CompiledValidator.Compile(schema);

		// Act / Assert — behaviour must be identical to calling Validate directly.
		await Assert.That(validator("abcd").IsSuccess).IsEqualTo(schema.Validate("abcd").IsSuccess);
		await Assert.That(validator("ab").IsSuccess).IsEqualTo(schema.Validate("ab").IsSuccess);
		await Assert.That(validator("ab").IsSuccess).IsFalse();
	}

	[Test]
	public async Task Compile_GivenNullSchema_Throws()
	{
		await Assert.That(() => CompiledValidator.Compile<string>(null!)).Throws<ArgumentNullException>();
	}

	[Test]
	public async Task CompileParser_GivenSameSchemaInstance_ReturnsTheSameDelegate()
	{
		// Arrange
		var schema = Z.String().Min(3);

		// Act / Assert
		await Assert
			.That(ReferenceEquals(CompiledValidator.CompileParser(schema), CompiledValidator.CompileParser(schema)))
			.IsTrue();
	}

	[Test]
	public async Task CompileParser_GivenInvalidInput_ThrowsZodException()
	{
		// Arrange
		var parser = CompiledValidator.CompileParser(Z.String().Min(3));

		// Act / Assert
		await Assert.That(parser("abcd")).IsEqualTo("abcd");
		await Assert.That(() => parser("ab")).Throws<ZodException>();
	}

	[Test]
	public async Task CompileParser_GivenNullSchema_Throws()
	{
		await Assert.That(() => CompiledValidator.CompileParser<string>(null!)).Throws<ArgumentNullException>();
	}
}
