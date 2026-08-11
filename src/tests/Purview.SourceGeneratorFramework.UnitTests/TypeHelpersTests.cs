using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;

namespace Purview.SourceGeneratorFramework.UnitTests;

public class TypeHelpersTests
{
	[Test]
	public async Task IsAttribute_TypeNameEndsWithAttribute_ReturnsTrue() =>
		await Assert.That(TypeHelpers.IsAttribute("MyAttribute")).IsTrue();

	[Test]
	public async Task IsAttribute_TypeNameWithoutSuffix_ReturnsFalse() =>
		await Assert.That(TypeHelpers.IsAttribute("MyClass")).IsFalse();

	[Test]
	public async Task GetTypeName_AttributeType_TrimsSuffix() =>
		await Assert.That(TypeHelpers.GetTypeName("MyAttribute")).IsEqualTo("My");

	[Test]
	public async Task GetTypeName_NonAttributeType_ReturnsOriginal() =>
		await Assert.That(TypeHelpers.GetTypeName("MyClass")).IsEqualTo("MyClass");

	[Test]
	public async Task IsValidIdentifier_ValidIdentifier_ReturnsTrue()
	{
		await Assert.That(TypeHelpers.IsValidIdentifier("validName")).IsTrue();
		await Assert.That(TypeHelpers.IsValidIdentifier("_validName")).IsTrue();
	}

	[Test]
	[Arguments("123invalid")]
	[Arguments("")]
	[Arguments(null)]
	public async Task IsValidIdentifier_InvalidIdentifier_ReturnsFalse(string? name)
	{
		await Assert.That(TypeHelpers.IsValidIdentifier(name)).IsFalse();
	}

	[Test]
	public async Task TryGetSpecialType_KnownKeyword_ReturnsTrue()
	{
		var result = TypeHelpers.TryGetSpecialType("int", out var specialType);

		await Assert.That(result).IsTrue();
		await Assert.That(specialType).IsEqualTo(SpecialType.System_Int32);
	}

	[Test]
	public async Task TryGetSpecialType_UnknownKeyword_ReturnsFalse()
	{
		var result = TypeHelpers.TryGetSpecialType("unknown", out _);

		await Assert.That(result).IsFalse();
	}
}
