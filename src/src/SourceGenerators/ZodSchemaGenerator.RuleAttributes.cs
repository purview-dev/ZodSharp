using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	/// <summary>
	/// Builds the pipeline that turns rules marked with the parameterless <c>[ZodRule]</c> into
	/// DataAnnotations-style validation attributes.
	/// </summary>
	internal static IncrementalValuesProvider<GeneratorResult<RuleAttributeGenerationModel>> GetRuleAttributeProvider(
		IncrementalGeneratorInitializationContext context
	) =>
		IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.ZodSharp.Core.ZodRuleAttribute,
			predicate: static (node, _) => node is TypeDeclarationSyntax,
			transform: static (attributeContext, cancellationToken) =>
				GetRuleAttributeModel(attributeContext, cancellationToken)
		);

	internal static void EmitRuleAttributes(
		SourceProductionContext context,
		SchemaGenerationModel model,
		ImmutableArray<GeneratorResult<RuleAttributeGenerationModel>> rules
	)
	{
		foreach (var rule in rules)
		{
			foreach (var diagnostic in rule.Diagnostics)
				context.ReportDiagnostic(diagnostic.ToDiagnostic());

			if (!rule.ShouldProcess)
				continue;

			var writer = model.Context.CreateCodeWriter();
			BuildRuleAttribute(writer, rule.Value);
			context.AddSource($"{rule.Value.AttributeType.Namespace}.{rule.Value.AttributeType.Name}.g.cs", writer);
		}
	}

	static GeneratorResult<RuleAttributeGenerationModel> GetRuleAttributeModel(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (context.TargetSymbol is not INamedTypeSymbol target)
			return default;

		var attribute = context.Attributes[0];

		// The mapping form ([ZodRule(typeof(...))]) declares which rule an attribute addresses rather than
		// marking a rule; CustomRuleResolver resolves it per property. A hand-authored attribute must still
		// address every rule declared under the name it derives from, otherwise some usages of that attribute
		// resolve to no rule at all.
		if (attribute.ConstructorArguments.Length > 0)
			return GetMappingFormResult(target, attribute);

		var ruleType = target;

		// Only the parameterless form (marking a rule) is handled here.
		if (!IsValidationRule(ruleType))
			return default;

		if (ruleType.ContainingType is not null || ruleType.IsAbstract)
		{
			return GeneratorResult<RuleAttributeGenerationModel>.Create(
				default(RuleAttributeGenerationModel),
				ReportUnsupportedRuleAttributeGeneration(ruleType, "only non-nested, non-abstract rules are supported")
			);
		}

		// A generic rule is supported when it has a single type parameter: the generated attribute maps to the
		// open generic, so the same attribute serves a primitive member (resolved to the non-generic family
		// member) and a scalar value object (closed with that type).
		if (ruleType.IsGenericType && ruleType.Arity != 1)
		{
			return GeneratorResult<RuleAttributeGenerationModel>.Create(
				default(RuleAttributeGenerationModel),
				ReportUnsupportedRuleAttributeGeneration(
					ruleType,
					"only non-generic rules and generic rules with a single type parameter are supported"
				)
			);
		}

		var attributeName = ResolveAttributeName(ruleType, attribute);

		// A hand-authored declaration of the same name wins: the generator cannot emit a duplicate type and the
		// hand-authored declaration's own [ZodRule] mapping governs every usage. Report it so that re-mapping
		// is never silent.
		if (ruleType.ContainingNamespace.GetTypeMembers(attributeName) is { Length: > 0 } claimed)
		{
			return GeneratorResult<RuleAttributeGenerationModel>.Create(
				default(RuleAttributeGenerationModel),
				ReportableDiagnostic.Create(
					DiagnosticLibrary.RuleAttributeNameAlreadyDeclared,
					false,
					ruleType,
					ruleType.Name,
					attributeName,
					claimed[0].ToDisplayString()
				)
			);
		}

		// A generated attribute sharing its name with a System.ComponentModel.DataAnnotations attribute would be
		// ambiguous for any consumer importing both namespaces. Keep the DataAnnotations name and generate ours
		// under a "Zod" suffix (for example `[MinLengthZod]`), so the rule's own error identity (Code/Message)
		// stays usable instead of the rule being skipped entirely.
		if (
			context.SemanticModel.Compilation.GetTypeByMetadataName(
				$"{TypeLibraryGenerator.SystemDataAnnotations}.{attributeName}"
			)
			is not null
		)
		{
			attributeName = attributeName.EndsWith("Attribute", StringComparison.Ordinal)
				? attributeName.Substring(0, attributeName.Length - "Attribute".Length) + "ZodAttribute"
				: attributeName + "Zod";
		}

		// Both halves of a rule pair derive the same attribute name. The open generic mapping subsumes the
		// non-generic one, so only the generic rule emits the attribute.
		if (!ruleType.IsGenericType && HasMarkedGenericSibling(ruleType, attributeName))
			return default;

		// A code/origin constructor parameter is only surfaced through IZodRule: the generated validation
		// supplies the value as a constructor argument and reads it back through the interface, so without the
		// interface the parameter is inert. Report it and keep generating, so the attribute still exists.
		ImmutableArray<ReportableDiagnostic> identityWarnings = [];
		if (
			!CustomRuleResolver.IsZodRule(ruleType)
			&& CustomRuleResolver.TryGetIdentityParameter(ruleType, out var identityParameter)
		)
		{
			identityWarnings =
			[
				ReportableDiagnostic.Create(
					DiagnosticLibrary.RuleIdentityParameterNotImplemented,
					false,
					ruleType,
					ruleType.Name,
					identityParameter
				),
			];
		}

		var ruleConstructors = CustomRuleResolver.SelectAttributeConstructors(ruleType);
		var properties = ImmutableArray.CreateBuilder<GeneratedAttributeProperty>();
		HashSet<string> propertyNames = [with(StringComparer.OrdinalIgnoreCase)];
		var constructors = ImmutableArray.CreateBuilder<GeneratedAttributeConstructor>();
		var messageParameter = ruleConstructors
			.SelectMany(static constructor => constructor.Parameters)
			.FirstOrDefault(CustomRuleResolver.IsMessageParameter);

		// Every attribute-addressable rule overload surfaces its own attribute constructor. Overloads that
		// differ only in the message/identity parameters collapse to the same parameterless attribute
		// constructor, so identical signatures are emitted once.
		foreach (var ruleConstructor in ruleConstructors)
		{
			if (
				!TryBuildAttributeConstructorModel(
					ruleConstructor,
					ruleType,
					context.SemanticModel.Compilation,
					identityWarnings,
					properties,
					propertyNames,
					out var generatedConstructor,
					out var failureDiagnostics
				)
			)
			{
				return GeneratorResult<RuleAttributeGenerationModel>.Create(default, failureDiagnostics);
			}

			if (!constructors.Any(existing => HaveSameParameters(existing.Parameters, generatedConstructor.Parameters)))
				constructors.Add(generatedConstructor);
		}

		if (messageParameter is not null && propertyNames.Add("Message"))
		{
			properties.Add(
				new GeneratedAttributeProperty(
					new TypeIdentity(TypeHelpers.StripNullableAnnotations(messageParameter.Type)),
					"Message",
					BuildMessageInitializer(messageParameter, ruleType),
					TypeHelpers.CanBeNull(messageParameter.Type)
				)
			);
		}

		return GeneratorResult<RuleAttributeGenerationModel>.Create(
			new RuleAttributeGenerationModel(
				new TypeIdentity(ruleType),
				new TypeIdentity(attributeName, ruleType.ContainingNamespace.ToDisplayString()),
				ruleType.DeclaredAccessibility == Accessibility.Public
					? TypeDeclarationAccessibility.Public
					: TypeDeclarationAccessibility.Internal,
				GetNamedString(attribute, "Code"),
				GetNamedString(attribute, "Origin"),
				GetNamedBool(attribute, "AllowMultiple"),
				new(properties.ToImmutable()),
				new(constructors.ToImmutable())
			),
			identityWarnings
		);
	}

	/// <summary>
	/// Builds the generated attribute constructor that mirrors one attribute-addressable rule overload, adding
	/// the overload's properties to the shared property set. A parameter the rule declares without a default has
	/// no default here either, so the attribute cannot be applied without it; the rule's <c>message</c> and
	/// identity (<c>code</c>/<c>origin</c>) parameters stay properties.
	/// </summary>
	/// <param name="ruleConstructor">The rule overload to mirror.</param>
	/// <param name="ruleType">The rule the attribute maps to.</param>
	/// <param name="compilation">The compilation, used to resolve the surfaced type of a generic parameter.</param>
	/// <param name="identityWarnings">Diagnostics to carry alongside an unsupported-parameter failure.</param>
	/// <param name="properties">The shared generated attribute property set.</param>
	/// <param name="propertyNames">The property names already added, so an overload pair does not duplicate one.</param>
	/// <param name="constructor">The generated constructor when the overload can be surfaced.</param>
	/// <param name="failureDiagnostics">The diagnostics to report when a parameter cannot be surfaced.</param>
	/// <returns><see langword="true"/> when the overload was surfaced.</returns>
	static bool TryBuildAttributeConstructorModel(
		IMethodSymbol ruleConstructor,
		INamedTypeSymbol ruleType,
		Compilation compilation,
		ImmutableArray<ReportableDiagnostic> identityWarnings,
		ImmutableArray<GeneratedAttributeProperty>.Builder properties,
		HashSet<string> propertyNames,
		out GeneratedAttributeConstructor constructor,
		out ImmutableArray<ReportableDiagnostic> failureDiagnostics
	)
	{
		constructor = default;
		failureDiagnostics = [];
		var constructorParameters = ImmutableArray.CreateBuilder<GeneratedAttributeParameter>();

		foreach (var parameter in ruleConstructor.Parameters)
		{
			// The rule's message flows through the inherited ValidationAttribute.ErrorMessage, which the
			// resolver maps onto this parameter. The generated attribute additionally exposes a Message alias,
			// so the rule's own parameter name stays usable at the call site.
			if (CustomRuleResolver.IsMessageParameter(parameter))
				continue;

			if (CustomRuleResolver.IsCancellationToken(parameter.Type))
				continue;

			var isIdentityParameter = CustomRuleResolver.IsIdentityParameter(parameter);

			// A type-parameter constructor argument (for example `MinValueRule<T>(T minValue)`) cannot be
			// mirrored directly; it is surfaced as a double, the numeric type the schema pipeline uses and the
			// type a `[MinValue]`-style attribute can carry.
			var propertyType =
				parameter.Type.TypeKind == TypeKind.TypeParameter
					? compilation.GetSpecialType(SpecialType.System_Double)
					: parameter.Type;

			if (!CustomRuleResolver.IsSupportedAttributePropertyType(propertyType))
			{
				failureDiagnostics =
				[
					ReportUnsupportedRuleAttributeGeneration(
						ruleType,
						$"constructor parameter '{parameter.Name}' has type '{parameter.Type.ToDisplayString()}', which cannot be represented as an attribute property"
					),
					.. identityWarnings,
				];

				return false;
			}

			TypeIdentity propertyTypeIdentity = new(TypeHelpers.StripNullableAnnotations(propertyType));
			var propertyName = ToPascalCase(parameter.Name);
			var initializer = BuildPropertyInitializer(parameter, propertyType, ruleType, isIdentityParameter);
			var isNullable = TypeHelpers.CanBeNull(propertyType);

			// The widest overload is visited first, so its property type and initializer win when an overload
			// pair shares a parameter.
			if (propertyNames.Add(propertyName))
			{
				properties.Add(
					new GeneratedAttributeProperty(propertyTypeIdentity, propertyName, initializer, isNullable)
				);
			}

			// The attribute constructor mirrors the rule's value parameters so a required value (for example
			// the `maxValue` of `LessThanOrEqualRule<T>`) has to be supplied at the usage site instead of
			// silently falling back to the type's default. A defaulted parameter keeps its default, so
			// `[Even]`-style attributes stay applicable without arguments.
			//
			// Identity parameters stay properties only: the resolver reads the reported error identity from the
			// applied attribute's named arguments, so a constructor parameter would never feed it.
			if (isIdentityParameter)
				continue;

			constructorParameters.Add(
				new GeneratedAttributeParameter(
					propertyTypeIdentity,
					parameter.Name,
					propertyName,
					parameter.HasExplicitDefaultValue ? initializer : null,
					isNullable
				)
			);
		}

		constructor = new GeneratedAttributeConstructor(constructorParameters.ToImmutable());
		return true;
	}

	/// <summary>
	/// Determines whether two generated attribute constructor signatures are identical, so an overload pair that
	/// differs only in message/identity parameters is emitted once.
	/// </summary>
	/// <param name="left">The first constructor's parameters.</param>
	/// <param name="right">The second constructor's parameters.</param>
	/// <returns><see langword="true"/> when the signatures match.</returns>
	static bool HaveSameParameters(
		EquatableArray<GeneratedAttributeParameter> left,
		EquatableArray<GeneratedAttributeParameter> right
	)
	{
		if (left.Count != right.Count)
			return false;

		for (var i = 0; i < left.Count; i++)
		{
			if (left[i] != right[i])
				return false;
		}

		return true;
	}

	static bool IsValidationRule(INamedTypeSymbol ruleType) =>
		ruleType.AllInterfaces.Any(static iface =>
			iface.OriginalDefinition is { Name: "IValidationRule", Arity: 1 } definition
			&& definition.ContainingNamespace.ToDisplayString() == TypeLibraryGenerator.ZodSharpCoreNamespace
		);

	/// <summary>
	/// Determines whether <paramref name="ruleType"/> is a built-in rule shipped in the runtime assembly. A
	/// generation failure for one of those is a documented library decision, so it is reported as ZODSGEN043
	/// rather than as the error a custom rule's author would need.
	/// </summary>
	static bool IsBuiltInRule(INamedTypeSymbol ruleType) =>
		ruleType.ContainingNamespace.ToDisplayString() == TypeLibraryGenerator.ZodSharpRulesNamespace;

	/// <summary>
	/// Reports a rule that cannot produce a validation attribute: <c>ZODSGEN043</c> (informational, not blocking)
	/// for a built-in rule whose gap is visible in the shipped build, and <c>ZODSGEN032</c> (error) for a custom
	/// rule whose author marked it deliberately.
	/// </summary>
	/// <param name="ruleType">The marked rule.</param>
	/// <param name="reason">Why the attribute cannot be generated.</param>
	/// <returns>The diagnostic to carry out of the pipeline.</returns>
	static ReportableDiagnostic ReportUnsupportedRuleAttributeGeneration(INamedTypeSymbol ruleType, string reason)
	{
		var isBuiltIn = IsBuiltInRule(ruleType);

		return ReportableDiagnostic.Create(
			isBuiltIn
				? DiagnosticLibrary.BuiltInRuleAttributeNotGenerated
				: DiagnosticLibrary.UnsupportedRuleAttributeGeneration,
			!isBuiltIn,
			ruleType,
			ruleType.Name,
			reason
		);
	}

	static string BuildInitializer(IParameterSymbol parameter, ITypeSymbol propertyType)
	{
		if (
			parameter.HasExplicitDefaultValue
			&& CustomRuleResolver.TryConvertValue(parameter.ExplicitDefaultValue, propertyType, out var literal)
		)
		{
			return literal;
		}

		// If the parameter has no default value, we still need to provide an initializer for the attribute property.
		return propertyType.IsValueType ? "default!" : "null!";
	}

	static string ToPascalCase(string name) =>
		name.Length == 0 ? name : string.Concat(char.ToUpperInvariant(name[0]), name.AsSpan(1).ToString());

	/// <summary>
	/// Reads a <c>public const string</c> member (for example a rule's <c>ErrorCode</c> or
	/// <c>MessageFormat</c>) so the generated attribute can mirror the rule's own default.
	/// </summary>
	/// <param name="ruleType">The rule type to inspect.</param>
	/// <param name="memberName">The name of the constant member.</param>
	/// <returns>The constant's value, or <see langword="null"/> when no such constant exists.</returns>
	static string? GetRuleConstString(INamedTypeSymbol ruleType, string memberName)
	{
		foreach (var member in ruleType.GetMembers(memberName))
		{
			if (member is IFieldSymbol { IsConst: true, ConstantValue: string value })
				return value;
		}

		return null;
	}

	/// <summary>
	/// Builds the initializer for a generated attribute property. The identity <c>Code</c> property mirrors
	/// the rule's own <c>ErrorCode</c> default so the generated attribute is self-describing; every other
	/// property keeps the parameter's own default.
	/// </summary>
	/// <param name="parameter">The rule constructor parameter the property mirrors.</param>
	/// <param name="propertyType">The property type.</param>
	/// <param name="ruleType">The rule type.</param>
	/// <param name="isIdentityParameter">Whether the parameter is the rule's <c>code</c>/<c>origin</c>.</param>
	/// <returns>The initializer expression.</returns>
	static string BuildPropertyInitializer(
		IParameterSymbol parameter,
		ITypeSymbol propertyType,
		INamedTypeSymbol ruleType,
		bool isIdentityParameter
	)
	{
		if (
			isIdentityParameter
			&& string.Equals(parameter.Name, "code", StringComparison.OrdinalIgnoreCase)
			&& GetRuleConstString(ruleType, "ErrorCode") is { } errorCode
		)
		{
			return errorCode.StringLiteral();
		}

		// The rule's own default is used for every other property, so the generated attribute is self-describing and
		return BuildInitializer(parameter, propertyType);
	}

	/// <summary>
	/// Builds the initializer for the generated <c>Message</c> property, mirroring the rule's own
	/// <c>MessageFormat</c> default when the rule exposes one.
	/// </summary>
	/// <param name="messageParameter">The rule's <c>message</c> constructor parameter.</param>
	/// <param name="ruleType">The rule type.</param>
	/// <returns>The initializer expression.</returns>
	static string BuildMessageInitializer(IParameterSymbol messageParameter, INamedTypeSymbol ruleType) =>
		GetRuleConstString(ruleType, "MessageFormat") is { } messageFormat
			? messageFormat.StringLiteral()
			: BuildInitializer(messageParameter, messageParameter.Type);

	static string? GetNamedString(AttributeData attribute, string name)
	{
		foreach (var pair in attribute.NamedArguments)
		{
			if (pair.Key == name)
				return pair.Value.Value as string;
		}

		return null;
	}

	static bool GetNamedBool(AttributeData attribute, string name)
	{
		foreach (var pair in attribute.NamedArguments)
		{
			if (pair.Key == name && pair.Value.Value is bool value)
				return value;
		}

		return false;
	}

	/// <summary>
	/// Validates the <c>[ZodRule(typeof(...))]</c> mapping form: an attribute whose name encodes a rule name
	/// (<c>XAttribute</c> → <c>XRule</c>) must address every rule declared under that name.
	/// </summary>
	/// <param name="attributeClass">The type carrying the mapping.</param>
	/// <param name="attribute">The <c>[ZodRule(typeof(...))]</c> mapping.</param>
	/// <returns>
	/// A result carrying ZODSGEN038 when the mapping leaves family members unaddressed, and ZODSGEN039 when the
	/// mapped rule declares an identity parameter it cannot surface.
	/// </returns>
	static GeneratorResult<RuleAttributeGenerationModel> GetMappingFormResult(
		INamedTypeSymbol attributeClass,
		AttributeData attribute
	)
	{
		if (
			!TypeHelpers.InheritsFrom(
				attributeClass,
				TypeLibrary.System.ComponentModel.DataAnnotations.ValidationAttribute
			) || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol mapped
		)
		{
			return default;
		}

		ImmutableArray<ReportableDiagnostic>.Builder? diagnostics = null;

		if (TryGetIncompleteFamilyCoverage(attributeClass, mapped, out var expectedMapping))
		{
			diagnostics = ImmutableArray.CreateBuilder<ReportableDiagnostic>();
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.RuleAttributeMappingIncomplete,
					false,
					attributeClass,
					attributeClass.Name,
					mapped.Name,
					expectedMapping.Name,
					expectedMapping.RenderFullName
				)
			);
		}

		// The mapped rule is the one that must surface the identity. A rule marked with [ZodRule] is already
		// checked at its own declaration, so only unmarked rules are reported here.
		var ruleDefinition = mapped.OriginalDefinition ?? mapped;
		if (
			!CustomRuleResolver.IsRuleMarker(ruleDefinition)
			&& !CustomRuleResolver.IsZodRule(ruleDefinition)
			&& CustomRuleResolver.TryGetIdentityParameter(ruleDefinition, out var identityParameter)
		)
		{
			diagnostics ??= ImmutableArray.CreateBuilder<ReportableDiagnostic>();
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.RuleIdentityParameterNotImplemented,
					false,
					ruleDefinition,
					ruleDefinition.Name,
					identityParameter
				)
			);
		}

		return diagnostics is null
			? default
			: GeneratorResult<RuleAttributeGenerationModel>.Create(default, diagnostics.ToImmutable());
	}

	/// <summary>
	/// Determines whether a rule attribute's mapping fails to address every rule declared as
	/// <c>{attribute-name-without-Attribute}Rule</c>.
	/// </summary>
	/// <param name="attributeClass">The attribute carrying the mapping.</param>
	/// <param name="mapped">The rule type the mapping declares.</param>
	/// <param name="expectedMapping">
	/// The mapping that would address the whole family: the arity-1 open generic when the family declares one,
	/// otherwise the non-generic rule.
	/// </param>
	/// <returns>
	/// <see langword="true"/> when the mapping leaves a family member unaddressed; <see langword="false"/> when
	/// it covers the family or the attribute name does not encode a declared rule family.
	/// </returns>
	/// <remarks>
	/// The check is skipped when no rule is declared under the encoded name, so an attribute may be named
	/// freely and mapped to any rule. When a family does exist, only the open generic reaches both halves of a
	/// primitive / scalar value-object pair, and an arity of two or more can never be closed from an attribute.
	/// </remarks>
	static bool TryGetIncompleteFamilyCoverage(
		INamedTypeSymbol attributeClass,
		INamedTypeSymbol mapped,
		out TypeIdentity expectedMapping
	)
	{
		const string attributeSuffix = "Attribute";
		const string ruleSuffix = "Rule";

		var attributeName = attributeClass.Name;
		var baseName =
			attributeName.EndsWith(attributeSuffix, StringComparison.Ordinal)
			&& attributeName.Length > attributeSuffix.Length
				? attributeName.Substring(0, attributeName.Length - attributeSuffix.Length)
				: attributeName;

		expectedMapping = default;

		var definition = mapped.OriginalDefinition ?? mapped;
		var namespaceName = definition.ContainingNamespace.ToDisplayString();
		var family = definition.ContainingNamespace.GetTypeMembers($"{baseName}{ruleSuffix}");

		// The attribute name does not encode a rule family, so there is nothing for the mapping to cover.
		if (family.Length == 0)
			return false;

		var hasOpenGeneric = family.Any(static member => member.Arity == 1);
		expectedMapping = new TypeIdentity($"{baseName}{ruleSuffix}", namespaceName, hasOpenGeneric ? 1 : 0);

		var isOpenGeneric =
			mapped.IsUnboundGenericType
			|| (
				mapped.IsGenericType
				&& mapped.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter)
			);

		// A differently named rule, or a family member whose arity can never be closed from an attribute, is
		// not addressable through this attribute.
		if (
			!string.Equals(definition.Name, expectedMapping.Name, StringComparison.Ordinal)
			|| family.Any(static member => member.Arity > 1)
		)
		{
			return true;
		}

		// A single closed instantiation cannot stand in for the open generic the rest of the family needs.
		return hasOpenGeneric && !isOpenGeneric;
	}

	/// <summary>
	/// Resolves the validation attribute name for a rule marked with <c>[ZodRule]</c>: the explicit
	/// <c>AttributeName</c> when set, otherwise the rule name with a trailing <c>Rule</c> replaced by
	/// <c>Attribute</c>.
	/// </summary>
	/// <param name="ruleType">The marked rule.</param>
	/// <param name="attribute">The <c>[ZodRule]</c> marker.</param>
	/// <returns>The validation attribute name declared by the marker.</returns>
	static string ResolveAttributeName(INamedTypeSymbol ruleType, AttributeData attribute)
	{
		const string ruleSuffix = "Rule";

		var explicitName = GetNamedString(attribute, "AttributeName");
		var baseName =
			explicitName is { Length: > 0 } ? explicitName
			: ruleType.Name.EndsWith(ruleSuffix, StringComparison.Ordinal) && ruleType.Name.Length > ruleSuffix.Length
				? ruleType.Name.Substring(0, ruleType.Name.Length - ruleSuffix.Length)
			: ruleType.Name;

		return $"{baseName}Attribute";
	}

	/// <summary>
	/// Determines whether the non-generic rule is the primitive half of a rule pair whose arity-1 generic
	/// sibling is marked with <c>[ZodRule]</c> for the same attribute name. The generic marker maps that
	/// attribute to the open generic rule, which serves both forms, so the non-generic rule must not emit a
	/// competing attribute (the two would share a hint name).
	/// </summary>
	/// <param name="ruleType">The marked non-generic rule.</param>
	/// <param name="attributeName">The attribute name the non-generic rule derives.</param>
	/// <returns><see langword="true"/> when a marked generic sibling emits the same attribute name.</returns>
	static bool HasMarkedGenericSibling(INamedTypeSymbol ruleType, string attributeName)
	{
		foreach (var candidate in ruleType.ContainingNamespace.GetTypeMembers(ruleType.Name))
		{
			if (candidate.Arity != 1 || !CustomRuleResolver.IsRuleMarker(candidate))
				continue;

			foreach (var attribute in candidate.GetAttributes())
			{
				if (!CustomRuleResolver.IsRuleMarkerAttribute(attribute))
					continue;

				if (string.Equals(ResolveAttributeName(candidate, attribute), attributeName, StringComparison.Ordinal))
				{
					return true;
				}
			}
		}

		return false;
	}

	static void BuildRuleAttribute(CodeWriter writer, RuleAttributeGenerationModel model)
	{
		writer.AutoGeneratedHeader();
		writer.Using("System").NewLine();
		writer.FileScopedNamespace(model.AttributeType.Namespace);

		writer.XmlSummary(
			$"Validation attribute that applies {CodeWriter.XmlSee(model.RuleType)}.",
			"Generated from the rule's [ZodRule] marker; the properties mirror the rule's constructor parameters."
		);

		using (
			writer.ClassScope(
				new TypeDeclarationOptions(model.AttributeType, model.Accessibility)
				{
					IsSealed = true,
					BaseType = TypeLibrary.System.ComponentModel.DataAnnotations.ValidationAttribute.AsTypeReference(),
					Attributes =
					[
						new AttributeDeclarationOptions(TypeLibrary.System.AttributeUsageAttribute)
						{
							Arguments =
							[
								new AttributeArgumentOptions(
									"global::System.AttributeTargets.Class"
										+ " | global::System.AttributeTargets.Struct"
										+ " | global::System.AttributeTargets.Property"
										+ " | global::System.AttributeTargets.Field"
										+ " | global::System.AttributeTargets.Parameter",
									null,
									false
								),
								new AttributeArgumentOptions("true", "Inherited", true),
								new AttributeArgumentOptions(
									model.AllowMultiple ? "true" : "false",
									"AllowMultiple",
									true
								),
							],
						},
						new AttributeDeclarationOptions(TypeLibrary.ZodSharp.Core.ZodRuleAttribute)
						{
							Arguments = BuildRuleArguments(model),
						},
					],
				}
			)
		)
		{
			// A single parameterless constructor is left implicit; once any constructor is emitted the
			// parameterless overload must be emitted explicitly so the attribute stays applicable without
			// arguments (for example `[Uuid]` alongside `[Uuid(UuidVersion.V4)]`).
			if (
				model.Constructors.Count > 1
				|| (model.Constructors.Count == 1 && model.Constructors[0].Parameters.Count > 0)
			)
			{
				foreach (var constructor in model.Constructors)
					BuildAttributeConstructor(writer, model, constructor);
			}

			foreach (var property in model.Properties)
			{
				var propertyType = property.IsNullable
					? property.Type.AsTypeReference().Nullable(writer)
					: property.Type.AsTypeReference();

				writer.Property(
					new PropertyDeclarationOptions(property.Name, propertyType, TypeDeclarationAccessibility.Public)
					{
						HasGetter = true,
						HasSetter = true,
						Initializer = property.Initializer,
					}
				);
			}
		}
	}

	/// <summary>
	/// Emits one attribute constructor that carries a rule overload's value parameters. A parameter the rule
	/// declares without a default has no default here either, so the attribute cannot be applied without it; the
	/// rule's <c>message</c> and identity (<c>code</c>/<c>origin</c>) parameters stay properties.
	/// </summary>
	/// <param name="writer">The writer positioned inside the attribute's class scope.</param>
	/// <param name="model">The generation model describing the attribute.</param>
	/// <param name="constructor">The constructor signature to emit.</param>
	static void BuildAttributeConstructor(
		CodeWriter writer,
		RuleAttributeGenerationModel model,
		GeneratedAttributeConstructor constructor
	)
	{
		writer.XmlSummary(
			$"Initializes the attribute with the values required by {CodeWriter.XmlSee(model.RuleType)}.",
			"A required rule constructor value cannot be omitted; optional values keep their rule default."
		);

		var parameters = ImmutableArray.CreateBuilder<ParameterDeclarationOptions>(constructor.Parameters.Count);

		foreach (var constructorParameter in constructor.Parameters)
		{
			var parameterType = constructorParameter.IsNullable
				? constructorParameter.Type.AsTypeReference().Nullable(writer)
				: constructorParameter.Type.AsTypeReference();

			parameters.Add(
				new ParameterDeclarationOptions(constructorParameter.Name, parameterType)
				{
					DefaultValue = constructorParameter.DefaultValue,
				}
			);
		}

		writer.Constructor(
			new ConstructorDeclarationOptions(model.AttributeType, TypeDeclarationAccessibility.Public)
			{
				Parameters = parameters.ToImmutable(),
			},
			constructorBody =>
			{
				foreach (var constructorParameter in constructor.Parameters)
					constructorBody.Assignment(constructorParameter.PropertyName, constructorParameter.Name);
			}
		);
	}

	static ImmutableArray<AttributeArgumentOptions> BuildRuleArguments(RuleAttributeGenerationModel model)
	{
		var builder = ImmutableArray.CreateBuilder<AttributeArgumentOptions>();
		builder.Add(new($"typeof({model.RuleType.RenderFullName})", null, false));

		if (model.Code is { Length: > 0 } code)
			builder.Add(new(code.StringLiteral(), "Code", true));

		if (model.Origin is { Length: > 0 } origin)
			builder.Add(new(origin.StringLiteral(), "Origin", true));

		return builder.ToImmutable();
	}
}
