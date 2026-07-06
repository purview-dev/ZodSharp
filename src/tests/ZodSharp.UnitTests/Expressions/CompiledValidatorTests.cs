using ZodSharp.Core;

namespace ZodSharp.Expressions;

public class CompiledValidatorTests
{
	[Test]
	public async Task Compile_GivenSchema_ResultMatchesValidateForValidInput()
	{
		var schema = Z.String().Min(3).Max(50).Email();
		var compiled = CompiledValidator.Compile(schema);

		var regularResult = schema.Validate("user@example.com");
		var compiledResult = compiled("user@example.com");

		await Assert.That(compiledResult.IsSuccess).IsEqualTo(regularResult.IsSuccess);
		await Assert.That(compiledResult.Value).IsEqualTo(regularResult.Value);
	}

	[Test]
	public async Task Compile_GivenSchema_ResultMatchesValidateForInvalidInput()
	{
		var schema = Z.String().Min(3).Max(50).Email();
		var compiled = CompiledValidator.Compile(schema);

		var regularResult = schema.Validate("no");
		var compiledResult = compiled("no");

		await Assert.That(compiledResult.IsSuccess).IsEqualTo(regularResult.IsSuccess);
	}

	[Test]
	public async Task CompileParser_GivenValidInput_ReturnsValue()
	{
		var parser = CompiledValidator.CompileParser(Z.String().Min(3));

		var value = parser("John");

		await Assert.That(value).IsEqualTo("John");
	}

	[Test]
	public async Task CompileParser_GivenInvalidInput_ThrowsZodException()
	{
		var parser = CompiledValidator.CompileParser(Z.String().Min(3));

		var exception = Assert.Throws<ZodException>(() => parser("AB"));

		await Assert.That(exception).IsNotNull();
	}
}
