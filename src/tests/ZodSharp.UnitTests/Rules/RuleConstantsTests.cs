using System.Reflection;

namespace ZodSharp.Rules;

/// <summary>
/// Guards the public error-identity constants every built-in rule exposes, so tests (and consumers) can
/// assert against a single source of truth instead of duplicating literal codes and message formats.
/// </summary>
public class RuleConstantsTests
{
	/// <summary>
	/// Every public struct in <see cref="ZodSharp.Rules"/> that implements
	/// <see cref="Core.IValidationRule{T}"/>.
	/// </summary>
	public static IEnumerable<Type> RuleTypes() =>
		typeof(IntRule)
			.Assembly.GetTypes()
			.Where(type => type is { IsPublic: true, IsValueType: true } && type.Namespace == "ZodSharp.Rules")
			.Where(type =>
				type.GetInterfaces()
					.Any(@interface =>
						@interface.IsGenericType
						&& @interface.GetGenericTypeDefinition() == typeof(Core.IValidationRule<>)
					)
			);

	/// <summary>The rule types that can be instantiated without closing an open generic.</summary>
	public static IEnumerable<Type> ConcreteRuleTypes() => RuleTypes().Where(type => !type.IsGenericTypeDefinition);

	[Test]
	[MethodDataSource(nameof(RuleTypes))]
	public async Task Rule_Always_ExposesErrorCodeAndMessageFormatConstants(Type ruleType)
	{
		// Act
		var errorCode = ruleType.GetField("ErrorCode", BindingFlags.Public | BindingFlags.Static);
		var messageFormat = ruleType.GetField("MessageFormat", BindingFlags.Public | BindingFlags.Static);

		// Assert — the constants exist, are compile-time literals, and are non-empty.
		await Assert.That(errorCode).IsNotNull();
		await Assert.That(messageFormat).IsNotNull();
		await Assert.That(errorCode!.IsLiteral).IsTrue();
		await Assert.That(messageFormat!.IsLiteral).IsTrue();
		await Assert.That((string)errorCode.GetRawConstantValue()!).IsNotEmpty();
		await Assert.That((string)messageFormat.GetRawConstantValue()!).IsNotEmpty();
	}

	[Test]
	[MethodDataSource(nameof(ConcreteRuleTypes))]
	public async Task Rule_Always_ReportsErrorCodeConstant(Type ruleType)
	{
		// Arrange — a default instance is enough: the Code property only returns the constant.
		var instance = Activator.CreateInstance(ruleType);

		// Act
		var code = (string?)ruleType.GetProperty("Code")!.GetValue(instance);
		var expected = (string)ruleType.GetField("ErrorCode")!.GetRawConstantValue()!;

		// Assert
		await Assert.That(code).IsEqualTo(expected);
	}
}
