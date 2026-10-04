namespace ZodSharp.SourceGenerators.Models;

/// <summary>
/// Describes the DataAnnotations-style validation attribute generated for a rule marked with
/// <c>[ZodRule]</c> (the parameterless form).
/// </summary>
/// <param name="RuleType">The rule the attribute maps to.</param>
/// <param name="AttributeType">The attribute type to emit.</param>
/// <param name="Accessibility">The accessibility of the generated attribute.</param>
/// <param name="Code">The error code the mapping reports, when explicitly configured.</param>
/// <param name="Origin">The structured origin the mapping reports, when explicitly configured.</param>
/// <param name="AllowMultiple">
/// Whether the generated attribute may be applied to a member more than once, taken from the rule marker's
/// <c>[ZodRule(AllowMultiple = ...)]</c>.
/// </param>
/// <param name="Properties">The generated attribute properties, mirroring the rule constructor parameters.</param>
/// <param name="ConstructorParameters">
/// The attribute constructor parameters carrying the rule's constructor values, in the rule's declaration
/// order. A parameter without a <see cref="GeneratedAttributeParameter.DefaultValue"/> is required, so the
/// attribute cannot be applied without it.
/// </param>
readonly record struct RuleAttributeGenerationModel(
	TypeIdentity RuleType,
	TypeIdentity AttributeType,
	TypeDeclarationAccessibility Accessibility,
	string? Code,
	string? Origin,
	bool AllowMultiple,
	EquatableArray<GeneratedAttributeProperty> Properties,
	EquatableArray<GeneratedAttributeParameter> ConstructorParameters = default
);

/// <summary>
/// Describes a settable property on a generated validation attribute.
/// </summary>
/// <param name="Type">The property type.</param>
/// <param name="Name">The property name.</param>
/// <param name="Initializer">The optional initializer expression.</param>
/// <param name="IsNullable">Whether the property type is nullable.</param>
readonly record struct GeneratedAttributeProperty(TypeIdentity Type, string Name, string? Initializer, bool IsNullable);

/// <summary>
/// Describes a constructor parameter on a generated validation attribute that carries a rule's constructor
/// value.
/// </summary>
/// <param name="Type">The parameter type.</param>
/// <param name="Name">The parameter name, taken from the rule's constructor parameter.</param>
/// <param name="PropertyName">The generated property the parameter assigns.</param>
/// <param name="DefaultValue">
/// The default-value expression for an optional parameter, or <see langword="null"/> when the parameter is
/// required and must be supplied at the attribute's usage site.
/// </param>
/// <param name="IsNullable">Whether the parameter type is nullable.</param>
readonly record struct GeneratedAttributeParameter(
	TypeIdentity Type,
	string Name,
	string PropertyName,
	string? DefaultValue,
	bool IsNullable
);
