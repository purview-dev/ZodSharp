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
/// <param name="Constructors">
/// The attribute constructors, one per attribute-addressable rule constructor, in the rule's declaration
/// order. A parameter without a <see cref="GeneratedAttributeParameter.DefaultValue"/> is required, so the
/// attribute cannot be applied without it. A rule with overloaded constructors (for example the versioned and
/// versionless UUID rules) surfaces one attribute constructor per overload so each rule overload stays
/// reachable.
/// </param>
readonly record struct RuleAttributeGenerationModel(
	TypeIdentity RuleType,
	TypeIdentity AttributeType,
	TypeDeclarationAccessibility Accessibility,
	string? Code,
	string? Origin,
	bool AllowMultiple,
	EquatableArray<GeneratedAttributeProperty> Properties,
	EquatableArray<GeneratedAttributeConstructor> Constructors = default
);

/// <summary>
/// Describes one constructor of a generated validation attribute, mirroring a single attribute-addressable
/// constructor of the rule it maps to.
/// </summary>
/// <param name="Parameters">
/// The constructor parameters carrying the rule's constructor values. Empty when the rule overload has no
/// attribute-addressable value parameters (its <c>message</c>/identity parameters are surfaced as properties),
/// which produces an explicit parameterless constructor when another constructor is also emitted.
/// </param>
readonly record struct GeneratedAttributeConstructor(EquatableArray<GeneratedAttributeParameter> Parameters);

/// <summary>
/// Describes a settable property on a generated validation attribute.
/// </summary>
/// <param name="Type">The property type. A <c>TypeReference</c> so an array parameter can be surfaced.</param>
/// <param name="Name">The property name.</param>
/// <param name="Initializer">The optional initializer expression.</param>
/// <param name="IsNullable">Whether the property type is nullable.</param>
readonly record struct GeneratedAttributeProperty(
	TypeReference Type,
	string Name,
	string? Initializer,
	bool IsNullable
);

/// <summary>
/// Describes a constructor parameter on a generated validation attribute that carries a rule's constructor
/// value.
/// </summary>
/// <param name="Type">The parameter type. A <c>TypeReference</c> so an array parameter can be surfaced.</param>
/// <param name="Name">The parameter name, taken from the rule's constructor parameter.</param>
/// <param name="PropertyName">The generated property the parameter assigns.</param>
/// <param name="DefaultValue">
/// The default-value expression for an optional parameter, or <see langword="null"/> when the parameter is
/// required and must be supplied at the attribute's usage site.
/// </param>
/// <param name="IsNullable">Whether the parameter type is nullable.</param>
/// <param name="IsParams">
/// Whether the parameter may be emitted as a <c>params</c> array. Only ever applied to the final parameter,
/// so an array-valued rule parameter stays ergonomic at the attribute's usage site.
/// </param>
readonly record struct GeneratedAttributeParameter(
	TypeReference Type,
	string Name,
	string PropertyName,
	string? DefaultValue,
	bool IsNullable,
	bool IsParams = false
);
