namespace ZodSharp.Core;

/// <summary>
/// Connects a <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> to the ZodSharp
/// validation rule that performs the validation, or marks a rule for generation of a matching
/// DataAnnotations-style attribute.
/// </summary>
/// <remarks>
/// <para>
/// The attribute has two roles:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Applied to a validation attribute (for example <c>NoWhitespaceAttribute</c>) it declares which rule
/// the attribute maps to: <c>[ZodRule(typeof(NoWhitespaceRule))]</c>. The <c>[ZodSchema]</c> generator
/// then emits rule-based validation for properties annotated with that attribute, exactly as it does for
/// the built-in DataAnnotations attributes.
/// </description>
/// </item>
/// <item>
/// <description>
/// Applied to a rule (for example <c>NoWhitespaceRule</c>) without a rule type it asks the generator to
/// emit a matching DataAnnotations-style attribute (<c>NoWhitespaceAttribute</c>) whose properties mirror
/// the rule's constructor parameters. An arity-1 generic rule may be marked as well: the generated
/// attribute maps to the open generic, which is how one attribute can serve both a primitive member and a
/// scalar value object.
/// </description>
/// </item>
/// </list>
/// <para>
/// The derived attribute name is the same for both halves of a generic / non-generic rule pair, so if the
/// compilation already declares a type with that name the generated attribute is suppressed and the
/// generator reports <c>ZODSGEN037</c>; the hand-authored declaration's own mapping then governs every
/// usage of that name. A hand-authored attribute whose name encodes a rule name (<c>XAttribute</c> →
/// <c>XRule</c>) must map to a rule that addresses every rule declared under that name, otherwise the
/// generator reports <c>ZODSGEN038</c>. Implement <see cref="IZodRuleAttribute"/> on a hand-authored
/// attribute to declare its <c>Code</c>/<c>Origin</c> as the reported error identity; an argument an
/// attribute supplies that the resolved rule cannot consume is reported as <c>ZODSGEN040</c> rather than
/// being dropped silently.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ZodRuleAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ZodRuleAttribute"/> class that marks a rule for
	/// validation-attribute generation.
	/// </summary>
	public ZodRuleAttribute() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="ZodRuleAttribute"/> class that maps a validation
	/// attribute to <paramref name="ruleType"/>.
	/// </summary>
	/// <param name="ruleType">The <c>IValidationRule&lt;T&gt;</c> implementation the attribute maps to.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="ruleType"/> is null.</exception>
	public ZodRuleAttribute(Type ruleType) => RuleType = ruleType ?? throw new ArgumentNullException(nameof(ruleType));

	/// <summary>
	/// Gets the rule type the attribute maps to, or <see langword="null"/> when the attribute marks a rule
	/// for validation-attribute generation.
	/// </summary>
	public Type? RuleType { get; }

	/// <summary>
	/// Gets the error code reported when the rule fails. When omitted, the generated validation reports
	/// <c>"validation_failed"</c>.
	/// </summary>
	public string? Code { get; init; }

	/// <summary>
	/// Gets the <see cref="ValidationError.Origin"/> reported when the rule fails.</summary>
	public string? Origin { get; init; }

	/// <summary>
	/// Gets the name of the validation attribute to generate when marking a rule. When omitted, the name is
	/// derived from the rule name (a trailing <c>Rule</c> is replaced with <c>Attribute</c>).
	/// </summary>
	public string? AttributeName { get; init; }

	/// <summary>
	/// Gets a value indicating whether the generated validation attribute may be applied to a member more than
	/// once, so a single rule can be configured differently per application.
	/// </summary>
	/// <remarks>
	/// Only affects generated attributes; a hand-authored attribute declares its own
	/// <see cref="AttributeUsageAttribute.AllowMultiple"/>. Every application is emitted as its own
	/// validation, evaluated in source order. Defaults to <see langword="false"/>, which is the
	/// <see cref="AttributeUsageAttribute"/> default.
	/// </remarks>
	public bool AllowMultiple { get; init; }
}
