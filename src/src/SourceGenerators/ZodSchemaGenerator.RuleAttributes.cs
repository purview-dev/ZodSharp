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
				ReportableDiagnostic.Create(
					DiagnosticLibrary.UnsupportedRuleAttributeGeneration,
					true,
					ruleType,
					ruleType.Name,
					"only non-nested, non-abstract rules are supported"
				)
			);
		}

		// A generic rule is supported when it has a single type parameter: the generated attribute maps to the
		// open generic, so the same attribute serves a primitive member (resolved to the non-generic family
		// member) and a scalar value object (closed with that type).
		if (ruleType.IsGenericType && ruleType.Arity != 1)
		{
			return GeneratorResult<RuleAttributeGenerationModel>.Create(
				default(RuleAttributeGenerationModel),
				ReportableDiagnostic.Create(
					DiagnosticLibrary.UnsupportedRuleAttributeGeneration,
					true,
					ruleType,
					ruleType.Name,
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

		var properties = ImmutableArray.CreateBuilder<GeneratedAttributeProperty>();
		var constructor = ruleType
			.InstanceConstructors.Where(static c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
			.OrderByDescending(static c => c.Parameters.Length)
			.FirstOrDefault();

		if (constructor is not null)
		{
			foreach (var parameter in constructor.Parameters)
			{
				if (IsMessageParameter(parameter) || IsCancellationToken(parameter.Type))
					continue;

				if (!IsSupportedAttributePropertyType(parameter.Type))
				{
					ImmutableArray<ReportableDiagnostic> propertyDiagnostics =
					[
						ReportableDiagnostic.Create(
							DiagnosticLibrary.UnsupportedRuleAttributeGeneration,
							true,
							ruleType,
							ruleType.Name,
							$"constructor parameter '{parameter.Name}' has type '{parameter.Type.ToDisplayString()}', which cannot be represented as an attribute property"
						),
						.. identityWarnings,
					];

					return GeneratorResult<RuleAttributeGenerationModel>.Create(default, propertyDiagnostics);
				}

				properties.Add(
					new GeneratedAttributeProperty(
						new TypeIdentity(TypeHelpers.StripNullableAnnotations(parameter.Type)),
						ToPascalCase(parameter.Name),
						BuildInitializer(parameter),
						TypeHelpers.CanBeNull(parameter.Type)
					)
				);
			}
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
				new(properties.ToImmutable())
			),
			identityWarnings
		);
	}

	static bool IsValidationRule(INamedTypeSymbol ruleType) =>
		ruleType.AllInterfaces.Any(static iface =>
			iface.OriginalDefinition is { Name: "IValidationRule", Arity: 1 } definition
			&& definition.ContainingNamespace.ToDisplayString() == TypeLibraryGenerator.ZodSharpCoreNamespace
		);

	static bool IsMessageParameter(IParameterSymbol parameter) =>
		string.Equals(parameter.Name, "message", StringComparison.OrdinalIgnoreCase);

	static bool IsCancellationToken(ITypeSymbol type) => type.ToDisplayString() == "System.Threading.CancellationToken";

	static string BuildInitializer(IParameterSymbol parameter)
	{
		if (
			parameter.HasExplicitDefaultValue
			&& CustomRuleResolver.TryConvertValue(parameter.ExplicitDefaultValue, parameter.Type, out var literal)
		)
		{
			return literal;
		}

		// If the parameter has no default value, we still need to provide an initializer for the attribute property.
		return parameter.Type.IsValueType ? "default!" : "null!";
	}

	static string ToPascalCase(string name) =>
		name.Length == 0 ? name : string.Concat(char.ToUpperInvariant(name[0]), name.AsSpan(1).ToString());

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

	static bool IsSupportedAttributePropertyType(ITypeSymbol type)
	{
		var unwrapped = TypeHelpers.UnwrapNullableType(type);
		if (unwrapped is INamedTypeSymbol { TypeKind: TypeKind.Enum })
			return true;

		if (unwrapped.ToDisplayString() == "System.Type")
			return true;

		// Only primitive types and string are supported as attribute properties.
		return unwrapped.SpecialType
			is SpecialType.System_Boolean
				or SpecialType.System_Byte
				or SpecialType.System_SByte
				or SpecialType.System_Char
				or SpecialType.System_Int16
				or SpecialType.System_UInt16
				or SpecialType.System_Int32
				or SpecialType.System_UInt32
				or SpecialType.System_Int64
				or SpecialType.System_UInt64
				or SpecialType.System_Single
				or SpecialType.System_Double
				or SpecialType.System_String;
	}

	static void BuildRuleAttribute(CodeWriter writer, RuleAttributeGenerationModel model)
	{
		writer.AutoGeneratedHeader();
		writer.Using("System").NewLine();
		writer.FileScopedNamespace(model.AttributeType.Namespace);

		writer.XmlSummary(
			$"Validation attribute that applies {CodeWriter.XmlSee(model.RuleType.Name)}.",
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
									"global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Parameter",
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
