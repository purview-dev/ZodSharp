#if NET11_0_OR_GREATER
namespace ZodSharp.Schemas;

/// <summary>
/// Runtime coverage for the net11-only native union schema. The analyzer and code-fix that point
/// consumers at <see cref="Z.NativeUnion{T1,T2}"/> are covered in SourceGenerators.UnitTests.
/// </summary>
public class ZodTypedNativeUnionTests
{
	[Test]
	public async Task NativeUnion_GivenReferenceTypeMatch_ReturnsNativeUnion()
	{
		// Arrange
		var schema = Z.NativeUnion(Z.String().Min(1), Z.Object().Build());

		// Act
		var result = schema.Validate("hello");

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value is string).IsTrue();
	}

	[Test]
	public async Task NativeUnion_GivenNoMatch_ReturnsInvalidUnionFailure()
	{
		// Arrange
		var schema = Z.NativeUnion(Z.String().Min(10), Z.Object().Build());

		// Act
		var result = schema.Validate("hi");

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("invalid_union");
	}

	[Test]
	public async Task NativeUnion_GivenConsumerSwitch_UnwrapsTheCase()
	{
		// Arrange
		var schema = Z.NativeUnion(Z.String().Min(1), Z.Object().Build());

		// Act
		var result = schema.Validate("hello");
		var length = result.IsSuccess
			? result.Value switch
			{
				string value => value.Length,
				Dictionary<string, object?> value => value.Count,
				_ => -1,
			}
			: -1;

		// Assert
		await Assert.That(length).IsEqualTo(5);
	}
}
#endif
