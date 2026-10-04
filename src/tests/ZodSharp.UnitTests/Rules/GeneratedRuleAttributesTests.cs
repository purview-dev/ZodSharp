using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ZodSharp.Core;

namespace ZodSharp.Rules;

/// <summary>
/// Guards the validation attributes the generator emits into the shipped <c>Purview.ZodSharp</c> assembly for
/// the built-in rules, so a consumer can annotate a member (or a scalar value object) with the rule's own
/// attribute instead of hand-authoring one.
/// </summary>
public class GeneratedRuleAttributesTests
{
	static Assembly RulesAssembly => typeof(IntRule<>).Assembly;

	/// <summary>The attribute name the rule derives: a trailing <c>Rule</c> becomes <c>Attribute</c>.</summary>
	static string AttributeName(string ruleName) =>
		(ruleName.EndsWith("Rule", StringComparison.Ordinal) ? ruleName[..^"Rule".Length] : ruleName) + "Attribute";

	static string AttributeName(Type ruleType) => AttributeName(ruleType.Name.Split('`')[0]);

	/// <summary>
	/// The rules whose derived attribute name is already taken by a <c>System.ComponentModel.DataAnnotations</c>
	/// attribute, so the generator emits theirs under the <c>Zod</c> suffix (for example <c>[MinLengthZod]</c>).
	/// </summary>
	static readonly Type[] CollisionRuleTypes =
	[
		typeof(MinLengthRule),
		typeof(MaxLengthRule),
		typeof(UrlRule),
		typeof(PhoneRule),
		typeof(CreditCardRule),
		typeof(Base64StringRule),
	];

	static string GeneratedAttributeName(Type ruleType)
	{
		var name = ruleType.Name.Split('`')[0];
		return AttributeName(CollisionRuleTypes.Contains(ruleType) ? name[..^"Rule".Length] + "ZodRule" : name);
	}

	static Type? FindAttribute(Type ruleType) =>
		RulesAssembly.GetType("ZodSharp.Rules." + GeneratedAttributeName(ruleType));

	/// <summary>
	/// Every built-in rule that generates a validation attribute. All built-in rules implement
	/// <see cref="IZodRule"/>, so each one owns its error identity and reports its own <c>ErrorCode</c>.
	/// </summary>
	public static IEnumerable<Type> AttributeBackedRuleTypes() =>
		RulesAssembly
			.GetTypes()
			.Where(static type => type is { IsPublic: true, IsValueType: true } && type.Namespace == "ZodSharp.Rules")
			.Where(static type => type.GetInterfaces().Contains(typeof(IZodRule)))
			.Where(static type => !type.IsGenericTypeDefinition || type.GetGenericArguments().Length == 1);

	public static IEnumerable<Type> DataAnnotationsCollisionRuleTypes() => CollisionRuleTypes;

	[Test]
	[MethodDataSource(nameof(AttributeBackedRuleTypes))]
	public async Task Rule_Always_HasGeneratedValidationAttribute(Type ruleType)
	{
		// Act
		var attributeType = FindAttribute(ruleType);

		// Assert
		await Assert.That(attributeType).IsNotNull();
		await Assert.That(attributeType!.IsSubclassOf(typeof(ValidationAttribute))).IsTrue();
		await Assert.That(attributeType.IsSealed).IsTrue();

		// The attribute maps back to the rule (the open generic for a generic rule), so the generator resolves
		// the rule from the applied attribute.
		var mapping = attributeType.GetCustomAttribute<ZodRuleAttribute>();
		await Assert.That(mapping).IsNotNull();
		await Assert.That(mapping!.RuleType).IsEqualTo(ruleType);
	}

	[Test]
	[MethodDataSource(nameof(AttributeBackedRuleTypes))]
	public async Task Attribute_Always_TargetsMembersAndScalarValueObjects(Type ruleType)
	{
		// Act
		var usage = FindAttribute(ruleType)!.GetCustomAttribute<AttributeUsageAttribute>();

		// Assert
		await Assert.That(usage).IsNotNull();
		await Assert.That(usage!.ValidOn.HasFlag(AttributeTargets.Class)).IsTrue();
		await Assert.That(usage.ValidOn.HasFlag(AttributeTargets.Struct)).IsTrue();
		await Assert.That(usage.ValidOn.HasFlag(AttributeTargets.Property)).IsTrue();
		await Assert.That(usage.ValidOn.HasFlag(AttributeTargets.Field)).IsTrue();
		await Assert.That(usage.ValidOn.HasFlag(AttributeTargets.Parameter)).IsTrue();
		await Assert.That(usage.Inherited).IsTrue();
		await Assert.That(usage.AllowMultiple).IsFalse();
	}

	[Test]
	[MethodDataSource(nameof(DataAnnotationsCollisionRuleTypes))]
	public async Task Rule_GivenDataAnnotationsNameCollision_GeneratesSuffixedAttribute(Type ruleType)
	{
		// Act
		var attributeType = FindAttribute(ruleType);

		// Assert - the plain name stays with DataAnnotations, and the rule's attribute uses the "Zod" suffix.
		await Assert
			.That(
				typeof(MinLengthAttribute).Assembly.GetType(
					"System.ComponentModel.DataAnnotations." + AttributeName(ruleType)
				)
			)
			.IsNotNull();
		await Assert.That(attributeType).IsNotNull();
		await Assert.That(attributeType!.Name).EndsWith("ZodAttribute", StringComparison.Ordinal);
		await Assert.That(attributeType.GetCustomAttribute<ZodRuleAttribute>()!.RuleType).IsEqualTo(ruleType);
	}

	[Test]
	[MethodDataSource(nameof(DataAnnotationsCollisionRuleTypes))]
	public async Task Rule_GivenDataAnnotationsNameCollision_ExposesCodeAndMessage(Type ruleType)
	{
		// Act
		var attributeType = FindAttribute(ruleType)!;

		// Assert - the suffixed attribute carries the rule's identity and message, so both can be overridden.
		await Assert.That(attributeType.GetProperty("Code")).IsNotNull();
		await Assert.That(attributeType.GetProperty("Message")).IsNotNull();
		await Assert.That(typeof(IZodRule).IsAssignableFrom(ruleType)).IsTrue();
	}

	[Test]
	[Arguments(typeof(MinValueRule<>), "MinValue")]
	[Arguments(typeof(MaxValueRule<>), "MaxValue")]
	[Arguments(typeof(GreaterThanRule<>), "ExclusiveMinimum")]
	[Arguments(typeof(LessThanRule<>), "ExclusiveMaximum")]
	[Arguments(typeof(GreaterThanOrEqualRule<>), "MinValue")]
	[Arguments(typeof(LessThanOrEqualRule<>), "MaxValue")]
	public async Task Attribute_GivenGenericBoundRule_ExposesDoubleBound(Type ruleType, string propertyName)
	{
		// Act - the type-parameter bound cannot be mirrored directly, so it is surfaced as a double.
		var property = FindAttribute(ruleType)!.GetProperty(propertyName);

		// Assert
		await Assert.That(property).IsNotNull();
		await Assert.That(property!.PropertyType).IsEqualTo(typeof(double));
	}

	[Test]
	[Arguments(typeof(RegexRule))]
	[Arguments(typeof(E164Rule))]
	[Arguments(typeof(NonSentinelRule<>))]
	[Arguments(typeof(GreaterThanOrEqualRule<>))]
	[Arguments(typeof(LessThanOrEqualRule<>))]
	[Arguments(typeof(EvenRule<>))]
	[Arguments(typeof(OddRule<>))]
	public async Task Attribute_GivenRuleWithMessageParameter_ExposesMessageAlias(Type ruleType)
	{
		// Act
		var message = FindAttribute(ruleType)!.GetProperty("Message");

		// Assert - the resolver maps Message onto the rule's message argument (ErrorMessage remains the fallback).
		await Assert.That(message).IsNotNull();
		await Assert.That(message!.PropertyType).IsEqualTo(typeof(string));
	}

	[Test]
	public async Task Attribute_GivenRuleWithoutMessageParameter_HasDefaultMessage()
	{
		// Arrange - the generated Message property mirrors the rule's own default message format.
		var attributeType = FindAttribute(typeof(EmailRule))!;
		var instance = Activator.CreateInstance(attributeType)!;

		// Act
		var message = (string?)attributeType.GetProperty("Message")!.GetValue(instance);

		// Assert
		await Assert.That(message).IsEqualTo(EmailRule.MessageFormat);
	}

	[Test]
	public async Task Attribute_GivenRuleWithoutCodeParameter_HasDefaultCode()
	{
		// Arrange - the generated Code property mirrors the rule's own default error code.
		var attributeType = FindAttribute(typeof(EmailRule))!;
		var instance = Activator.CreateInstance(attributeType)!;

		// Act
		var code = (string?)attributeType.GetProperty("Code")!.GetValue(instance);

		// Assert
		await Assert.That(code).IsEqualTo(EmailRule.ErrorCode);
	}

	[Test]
	public async Task Attribute_GivenRegexRule_MirrorsTheStringPatternOverload()
	{
		// Act - the Regex overload is not representable as an attribute property, so the string overload is used.
		var pattern = FindAttribute(typeof(RegexRule))!.GetProperty("Pattern");

		// Assert
		await Assert.That(pattern).IsNotNull();
		await Assert.That(pattern!.PropertyType).IsEqualTo(typeof(string));
	}
}
