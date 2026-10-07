using System.Reflection;

namespace ZodSharp.Core;

public class ZodSchemaGeneratedAttributeTests
{
	[Test]
	public async Task Attribute_TargetsModule_AndExposesTargetType()
	{
		ZodSchemaGeneratedAttribute attr = new(typeof(string), typeof(SampleDtoSchemaValidator));

		await Assert.That(attr.TargetType).IsEqualTo(typeof(string));
	}

	[Test]
	public async Task Attribute_ExposesTheValidatorType()
	{
		// The validator type is named directly rather than rebuilt from a string at runtime. That is what
		// makes RegisterFromAssembly trim- and AOT-safe: the typeof roots the validator for the trimmer.
		ZodSchemaGeneratedAttribute attr = new(typeof(SampleDto), typeof(SampleDtoSchemaValidator));

		await Assert.That(attr.ValidatorType).IsEqualTo(typeof(SampleDtoSchemaValidator));
	}

	[Test]
	public async Task Attribute_GivenNullValidatorType_Throws()
	{
		await Assert
			.That(() => new ZodSchemaGeneratedAttribute(typeof(SampleDto), null!))
			.Throws<ArgumentNullException>();
	}

	[Test]
	public async Task Attribute_ValidatorTypeIsAnnotatedForTrimming()
	{
		// Without this annotation the trimmer removes the validator's parameterless constructor and
		// Activator.CreateInstance fails at runtime in a published application.
		var property = typeof(ZodSchemaGeneratedAttribute).GetProperty(
			nameof(ZodSchemaGeneratedAttribute.ValidatorType)
		)!;
		var annotation =
			property.GetCustomAttribute<System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute>();

		await Assert.That(annotation).IsNotNull();
		await Assert
			.That(annotation!.MemberTypes)
			.IsEqualTo(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor);
	}

	[Test]
	public async Task Attribute_AllowMultipleAndNotInherited()
	{
		var usage = typeof(ZodSchemaGeneratedAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
		await Assert.That(usage.AllowMultiple).IsTrue();
		await Assert.That(usage.Inherited).IsFalse();
	}

	[Test]
	public async Task Attribute_ValidOn_ModuleClassAssembly()
	{
		var usage = typeof(ZodSchemaGeneratedAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
		await Assert
			.That(usage.ValidOn)
			.IsEqualTo(AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Assembly);
	}
}
