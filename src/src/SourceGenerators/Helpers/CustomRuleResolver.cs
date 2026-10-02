using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZodSharp.SourceGenerators.Models;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators.Helpers;

/// <summary>
/// Resolves DataAnnotations-style validation attributes that carry <c>[ZodRule(typeof(...))]</c> into
/// <see cref="CustomRuleDescriptor"/> values the schema generator can emit.
/// </summary>
/// <remarks>
/// <para>
/// Only attributes derived from <c>System.ComponentModel.DataAnnotations.ValidationAttribute</c> are
/// considered, so custom rules participate in the same "a property is validated when it carries a data
/// annotation" discovery as the built-in attributes.
/// </para>
/// <para>
/// The applied attribute's constructor arguments are mapped positionally and its named arguments by name
/// (case-insensitive) to the rule's public constructor parameters. A parameter named <c>message</c> is
/// supplied from the attribute's <c>ErrorMessage</c> when one is set.
/// </para>
/// </remarks>
static class CustomRuleResolver
{
	/// <summary>
	/// Resolves the custom rules declared by <paramref name="symbol"/>, discarding the diagnostics a failed
	/// resolution produces.
	/// </summary>
	/// <param name="symbol">The symbol whose rule attributes are resolved.</param>
	/// <param name="ruleTargetType">The type the rules are applied to.</param>
	/// <returns>The resolved rule descriptors.</returns>
	/// <remarks>
	/// Used by the generator, which only needs the descriptors: the same resolution runs in
	/// <c>ZodSchemaAnalyzer</c>, which reports the diagnostics, so a rule that resolves to nothing is still
	/// visible in the build.
	/// </remarks>
	public static EquatableArray<CustomRuleDescriptor> Resolve(ISymbol symbol, ITypeSymbol ruleTargetType) =>
		Resolve(symbol, ruleTargetType, ImmutableArray.CreateBuilder<ReportableDiagnostic>());

	public static EquatableArray<CustomRuleDescriptor> Resolve(
		ISymbol symbol,
		ITypeSymbol ruleTargetType,
		ImmutableArray<ReportableDiagnostic>.Builder diagnostics
	)
	{
		ImmutableArray<CustomRuleDescriptor>.Builder? builder = null;

		foreach (var attribute in symbol.GetAttributes())
		{
			if (attribute.AttributeClass is not INamedTypeSymbol attributeClass)
				continue;

			// The built-in DataAnnotations attributes have dedicated emitters.
			if (IsBuiltInDataAnnotation(attributeClass))
				continue;

			if (!TryGetRuleMapping(attributeClass, out var mapping))
				continue;

			if (!TryResolveRuleType(mapping.RuleType, ruleTargetType, out var ruleType))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.UnsupportedCustomRuleTarget,
						true,
						GetAttributeLocation(attribute),
						mapping.RuleType.Name,
						attributeClass.Name,
						ruleTargetType.ToDisplayString()
					)
				);
				continue;
			}

			if (!ImplementsRuleFor(ruleType, ruleTargetType))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.UnsupportedCustomRuleTarget,
						true,
						GetAttributeLocation(attribute),
						ruleType.Name,
						attributeClass.Name,
						ruleTargetType.ToDisplayString()
					)
				);
				continue;
			}

			if (!TryBuildArguments(attribute, ruleType, out var arguments, out var unmappedParameterName))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.UnmappableCustomRuleArgument,
						true,
						GetAttributeLocation(attribute),
						unmappedParameterName!,
						ruleType.Name,
						attributeClass.Name
					)
				);
				continue;
			}

			builder ??= ImmutableArray.CreateBuilder<CustomRuleDescriptor>();
			builder.Add(
				new CustomRuleDescriptor(
					new TypeIdentity(ruleType),
					GetAttributeString(attribute, "Code") ?? mapping.Code,
					GetAttributeString(attribute, "Origin") ?? mapping.Origin,
					ValidationAttributeData.FromAttributeData(attribute),
					arguments,
					IsZodRule(ruleType)
				)
			);

			foreach (var unusedArgument in GetUnusedNamedArguments(attribute, attributeClass, ruleType))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.UnusedRuleAttributeArgument,
						false,
						GetAttributeLocation(attribute),
						unusedArgument,
						attributeClass.Name,
						ruleType.Name
					)
				);
			}
		}

		return builder is null ? new(ImmutableArray<CustomRuleDescriptor>.Empty) : new(builder.ToImmutable());
	}

	static bool IsBuiltInDataAnnotation(INamedTypeSymbol attributeClass) =>
		attributeClass.ContainingNamespace.ToDisplayString() == TypeLibraryGenerator.SystemDataAnnotations;

	/// <summary>
	/// Determines whether <paramref name="attributeClass"/> is mapped to a validation rule through
	/// <c>[ZodRule(typeof(...))]</c>. Used by the analyzer to flag rule attributes that can never run.
	/// </summary>
	/// <param name="attributeClass">The attribute type to test.</param>
	/// <returns><see langword="true"/> when the attribute is mapped to a rule.</returns>
	internal static bool IsRuleMapped(INamedTypeSymbol attributeClass) => TryGetRuleMapping(attributeClass, out _);

	/// <summary>
	/// Determines whether <paramref name="ruleType"/> carries the parameterless <c>[ZodRule]</c> marker that
	/// asks the generator to emit a matching validation attribute.
	/// </summary>
	/// <param name="ruleType">The rule type to test.</param>
	/// <returns><see langword="true"/> when the rule is marked for validation-attribute generation.</returns>
	internal static bool IsRuleMarker(INamedTypeSymbol ruleType)
	{
		foreach (var attribute in ruleType.GetAttributes())
		{
			if (IsRuleMarkerAttribute(attribute))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Determines whether <paramref name="attribute"/> is the parameterless <c>[ZodRule]</c> marker (the form
	/// that marks a rule) rather than the mapping form that carries a rule type.
	/// </summary>
	/// <param name="attribute">The attribute to test.</param>
	/// <returns><see langword="true"/> when the attribute marks a rule for attribute generation.</returns>
	internal static bool IsRuleMarkerAttribute(AttributeData attribute) =>
		attribute.AttributeClass is not null
		&& attribute.AttributeClass.ToDisplayString()
			== $"{TypeLibraryGenerator.ZodSharpCoreNamespace}.ZodRuleAttribute"
		&& attribute.ConstructorArguments.Length == 0;

	/// <summary>
	/// Resolves the rule type to instantiate: a plain type is used as-is, an unbound generic
	/// (<c>[ZodRule(typeof(NotEmptyRule&lt;&gt;))]</c>) is closed with the property type, and a rule that cannot
	/// validate the property type falls back to another member of its family (the same base name declared
	/// alongside it) so a single attribute can serve both primitive members and scalar value objects.
	/// </summary>
	/// <param name="ruleType">The rule type declared by the mapping.</param>
	/// <param name="propertyType">The property type used to close an unbound generic rule.</param>
	/// <param name="resolved">The rule type to instantiate.</param>
	/// <returns><see langword="true"/> when a usable rule type was resolved.</returns>
	static bool TryResolveRuleType(INamedTypeSymbol ruleType, ITypeSymbol propertyType, out INamedTypeSymbol resolved)
	{
		resolved = ruleType;

		if (IsOpenGeneric(ruleType))
		{
			var definition = ruleType.OriginalDefinition;
			if (definition is null)
				return false;

			if (definition.Arity == 1 && SatisfiesConstraints(definition, propertyType))
			{
				resolved = definition.Construct(propertyType);
				return true;
			}
		}
		else if (ImplementsRuleFor(ruleType, propertyType))
		{
			return true;
		}

		// The declared rule cannot validate the property type. Prefer another member of the rule family under
		// the same base name: NonWhiteSpaceStringRule<TSelf> applied to a string member resolves to
		// NonWhiteSpaceStringRule, and NonWhiteSpaceStringRule applied to a scalar value object resolves to
		// NonWhiteSpaceStringRule<TScalar>. The caller still verifies the resolved rule implements
		// IValidationRule<TProperty>.
		return TryResolveFamilyMember(ruleType.OriginalDefinition ?? ruleType, propertyType, out resolved);
	}

	/// <summary>
	/// Finds the rule family member that can validate <paramref name="propertyType"/>: the non-generic rule
	/// (for example <c>NotEmptyRule</c>) or an arity-1 generic rule closed with the property type (for example
	/// <c>NotEmptyRule&lt;TScalar&gt;</c>).
	/// </summary>
	/// <param name="definition">The rule declared by the mapping.</param>
	/// <param name="propertyType">The property type the rule must validate.</param>
	/// <param name="resolved">The family member that validates the property type, when one exists.</param>
	/// <returns><see langword="true"/> when a usable family member was found.</returns>
	static bool TryResolveFamilyMember(
		INamedTypeSymbol definition,
		ITypeSymbol propertyType,
		out INamedTypeSymbol resolved
	)
	{
		foreach (var candidate in definition.ContainingNamespace.GetTypeMembers(definition.Name))
		{
			if (candidate.IsGenericType)
			{
				if (candidate.Arity != 1 || !SatisfiesConstraints(candidate, propertyType))
					continue;

				var closed = candidate.Construct(propertyType);
				if (!ImplementsRuleFor(closed, propertyType))
					continue;

				resolved = closed;
				return true;
			}

			if (!ImplementsRuleFor(candidate, propertyType))
				continue;

			resolved = candidate;
			return true;
		}

		resolved = definition;
		return false;
	}

	static bool IsOpenGeneric(INamedTypeSymbol type) =>
		type.IsGenericType
		&& (
			type.IsUnboundGenericType
			|| type.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter)
		);

	static bool SatisfiesConstraints(INamedTypeSymbol definition, ITypeSymbol type)
	{
		foreach (var parameter in definition.TypeParameters)
		{
			if (parameter.HasValueTypeConstraint && !type.IsValueType)
				return false;

			if (parameter.HasReferenceTypeConstraint && !type.IsReferenceType)
				return false;

			if (parameter.HasUnmanagedTypeConstraint && !type.IsUnmanagedType)
				return false;

			foreach (var constraint in parameter.ConstraintTypes)
			{
				// Substitute the candidate type into the constraint so self-referential constraints (for
				// example where TSelf : IScalarValueObject<TSelf, string>) are checked rather than assumed.
				// Assuming them lets an unsatisfiable rule reach emission, and the consumer then fails to
				// compile the generated validator (CS0311) instead of receiving a diagnostic.
				var substituted = SubstituteConstraint(constraint, parameter, type);

				// A constraint that references a type parameter which cannot be replaced from the definition
				// alone is treated as satisfied rather than rejecting a usable rule.
				if (substituted is null)
					continue;

				if (IsOrImplements(type, substituted))
					continue;

				// A constraint can be satisfied by a declaration this generator cannot see: a type declared
				// partial in this compilation may receive the interface implementation from another generator
				// (a scalar value object gets IScalarValueObject<TSelf, TValue> from the value object
				// generator, which does not run in this compilation's view). Defer those to the compiler
				// instead of dropping a rule that the consumer's compilation can satisfy.
				if (ContainsTypeParameter(constraint) && CanReceiveGeneratedMembers(type))
					continue;

				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Determines whether <paramref name="type"/> can be completed by another source generator, which means
	/// it can end up satisfying a constraint this compilation does not yet show.
	/// </summary>
	/// <param name="type">The candidate type to close the rule with.</param>
	/// <returns><see langword="true"/> when the type is declared <c>partial</c> in this compilation.</returns>
	/// <remarks>
	/// Only a type declared in the compilation being generated for can receive further declarations, and
	/// only a <c>partial</c> declaration can be extended. A primitive or a type from a referenced assembly
	/// can never gain the members a rule's constraint asks for, so it keeps the strict check.
	/// </remarks>
	static bool CanReceiveGeneratedMembers(ITypeSymbol type)
	{
		if (type is not INamedTypeSymbol named)
			return false;

		foreach (var reference in named.DeclaringSyntaxReferences)
		{
			if (
				reference.GetSyntax() is TypeDeclarationSyntax declaration
				&& declaration.Modifiers.Any(SyntaxKind.PartialKeyword)
			)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Replaces every occurrence of <paramref name="parameter"/> inside <paramref name="constraint"/> with
	/// <paramref name="type"/>.
	/// </summary>
	/// <param name="constraint">The declared constraint type.</param>
	/// <param name="parameter">The rule type parameter being closed.</param>
	/// <param name="type">The candidate type used to close the rule.</param>
	/// <returns>
	/// The substituted constraint, or <see langword="null"/> when it references a type parameter that is not
	/// <paramref name="parameter"/> and therefore cannot be evaluated from the definition alone.
	/// </returns>
	static ITypeSymbol? SubstituteConstraint(ITypeSymbol constraint, ITypeParameterSymbol parameter, ITypeSymbol type)
	{
		if (constraint.TypeKind == TypeKind.TypeParameter)
			return SymbolEqualityComparer.Default.Equals(constraint, parameter) ? type : null;

		if (constraint is not INamedTypeSymbol named)
			return constraint;

		var arguments = named.TypeArguments;
		if (arguments.Length == 0)
			return constraint;

		// An unbound generic constraint (for example IEquatable<>) carries no argument to substitute.
		if (named.IsUnboundGenericType)
			return null;

		var substituted = new ITypeSymbol[arguments.Length];
		var changed = false;

		for (var index = 0; index < arguments.Length; index++)
		{
			var argument = arguments[index];
			if (!ContainsTypeParameter(argument))
			{
				substituted[index] = argument;
				continue;
			}

			var replacement = SubstituteConstraint(argument, parameter, type);
			if (replacement is null)
				return null;

			substituted[index] = replacement;
			changed = true;
		}

		// A constructed constraint must be re-created from its definition; constructing from an already
		// constructed type (for example IScalarValueObject<TSelf, string>) is rejected by Roslyn.
		return changed ? named.OriginalDefinition.Construct(substituted) : named;
	}

	/// <summary>
	/// Determines whether <paramref name="type"/> is, implements or inherits <paramref name="contract"/>,
	/// including the contract's generic arguments.
	/// </summary>
	/// <remarks>
	/// Unlike <c>TypeHelpers.IsOrImplements</c>, which matches on the metadata name alone, this compares the
	/// constructed contract so
	/// <c>IScalarValueObject&lt;T, string&gt;</c> is not satisfied by a different construction.
	/// </remarks>
	static bool IsOrImplements(ITypeSymbol type, ITypeSymbol contract)
	{
		if (contract.TypeKind == TypeKind.TypeParameter)
			return true;

		if (SymbolEqualityComparer.Default.Equals(type, contract))
			return true;

		for (var baseType = (type as INamedTypeSymbol)?.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			if (SymbolEqualityComparer.Default.Equals(baseType, contract))
				return true;
		}

		foreach (var @interface in type.AllInterfaces)
		{
			if (SymbolEqualityComparer.Default.Equals(@interface, contract))
				return true;
		}

		return false;
	}

	static bool ContainsTypeParameter(ITypeSymbol type) =>
		type.TypeKind == TypeKind.TypeParameter
		|| (type is INamedTypeSymbol named && named.TypeArguments.Any(ContainsTypeParameter));

	/// <summary>
	/// Determines whether <paramref name="ruleType"/> owns its error identity by implementing
	/// <c>ZodSharp.Core.IZodRule</c>.
	/// </summary>
	/// <param name="ruleType">The resolved rule type.</param>
	/// <returns><see langword="true"/> when the rule supplies <c>Code</c>/<c>Origin</c> itself.</returns>
	internal static bool IsZodRule(INamedTypeSymbol ruleType) =>
		TypeHelpers.IsOrImplements(ruleType, TypeLibrary.ZodSharp.Core.IZodRule);

	/// <summary>
	/// Finds an identity constructor parameter (<c>code</c> or <c>origin</c>) declared by
	/// <paramref name="ruleType"/>.
	/// </summary>
	/// <param name="ruleType">The rule type to inspect.</param>
	/// <param name="parameterName">The first identity parameter found.</param>
	/// <returns><see langword="true"/> when the rule declares an identity constructor parameter.</returns>
	/// <remarks>
	/// Such a parameter is only surfaced when the rule implements <c>IZodRule</c>; otherwise the generated
	/// validation supplies the value and never reads it back, which the generator reports as ZODSGEN039.
	/// </remarks>
	internal static bool TryGetIdentityParameter(INamedTypeSymbol ruleType, out string parameterName)
	{
		foreach (var parameter in MappedParameters(ruleType))
		{
			if (
				string.Equals(parameter.Name, "code", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(parameter.Name, "origin", StringComparison.OrdinalIgnoreCase)
			)
			{
				parameterName = parameter.Name;
				return true;
			}
		}

		parameterName = string.Empty;
		return false;
	}

	/// <summary>
	/// Gets the named arguments an applied attribute supplies that the resolved rule never consumes: the
	/// argument matches no public constructor parameter and is not part of the reported error identity, so the
	/// value has no effect on validation.
	/// </summary>
	/// <param name="attribute">The applied attribute.</param>
	/// <param name="attributeClass">The attribute type.</param>
	/// <param name="ruleType">The rule the attribute resolves to.</param>
	/// <returns>The names of the arguments that have no effect, in declaration order.</returns>
	static ImmutableArray<string> GetUnusedNamedArguments(
		AttributeData attribute,
		INamedTypeSymbol attributeClass,
		INamedTypeSymbol ruleType
	)
	{
		if (attribute.NamedArguments.IsDefaultOrEmpty)
			return [];

		HashSet<string> consumed = [with(StringComparer.OrdinalIgnoreCase)];

		foreach (var parameter in MappedParameters(ruleType))
			consumed.Add(parameter.Name);

		// The inherited ValidationAttribute error-message members feed the emitted message directly.
		consumed.Add("ErrorMessage");
		consumed.Add("ErrorMessageResourceName");
		consumed.Add("ErrorMessageResourceType");

		// Identity properties are read from the applied attribute rather than passed to the rule.
		foreach (var identityProperty in GetIdentityPropertyNames(attributeClass))
			consumed.Add(identityProperty);

		ImmutableArray<string>.Builder? unused = null;
		foreach (var pair in attribute.NamedArguments)
		{
			if (consumed.Contains(pair.Key))
				continue;

			unused ??= ImmutableArray.CreateBuilder<string>();
			unused.Add(pair.Key);
		}

		return unused is null ? [] : unused.ToImmutable();
	}

	/// <summary>
	/// Gets the error-identity property names the generator reads from an applied attribute.
	/// </summary>
	/// <param name="attributeClass">The attribute type, when known.</param>
	/// <returns>The identity property names.</returns>
	/// <remarks>
	/// The contract is declared by <c>ZodSharp.Core.IZodRuleAttribute</c>: an attribute that implements it
	/// contributes its members, so a member added to the interface is treated as identity rather than as an
	/// unconsumed argument. Attributes that do not implement the interface are still read through the
	/// documented <c>Code</c>/<c>Origin</c> names so existing declarations keep working.
	/// </remarks>
	static ImmutableArray<string> GetIdentityPropertyNames(INamedTypeSymbol? attributeClass)
	{
		if (attributeClass is not null)
		{
			foreach (var @interface in attributeClass.AllInterfaces)
			{
				var definition = @interface.OriginalDefinition;
				if (
					definition.Name != IdentityAttributeInterfaceName
					|| definition.Arity != 0
					|| definition.ContainingNamespace.ToDisplayString() != TypeLibraryGenerator.ZodSharpCoreNamespace
				)
				{
					continue;
				}

				var names = @interface
					.GetMembers()
					.OfType<IPropertySymbol>()
					.Select(static property => property.Name)
					.ToImmutableArray();
				if (names.Length > 0)
					return names;
			}
		}

		return DocumentedIdentityPropertyNames;
	}

	/// <summary>
	/// Gets the public constructor parameters used to build the rule's arguments: the constructor with the most
	/// parameters, matching the rule instantiation contract.
	/// </summary>
	/// <param name="ruleType">The resolved rule type.</param>
	/// <returns>The mapped constructor parameters.</returns>
	static ImmutableArray<IParameterSymbol> MappedParameters(INamedTypeSymbol ruleType) =>
		ruleType
			.InstanceConstructors.Where(static c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
			.OrderByDescending(static c => c.Parameters.Length)
			.FirstOrDefault()
			?.Parameters
		?? [];

	/// <summary>
	/// The attribute contract that declares the error-identity properties, matched by name so the generator
	/// does not take a package reference on the public API.
	/// </summary>
	const string IdentityAttributeInterfaceName = "IZodRuleAttribute";

	/// <summary>
	/// The identity property names read from attributes that do not implement
	/// <c>ZodSharp.Core.IZodRuleAttribute</c>: the members that interface declares.
	/// </summary>
	static readonly ImmutableArray<string> DocumentedIdentityPropertyNames = ["Code", "Origin"];

	static string? GetAttributeString(AttributeData attribute, string name)
	{
		foreach (var pair in attribute.NamedArguments)
		{
			if (pair.Key == name)
				return pair.Value.Value as string;
		}

		return null;
	}

	static bool TryGetRuleMapping(INamedTypeSymbol attributeClass, out RuleMapping mapping)
	{
		for (var current = (INamedTypeSymbol?)attributeClass; current is not null; current = current.BaseType)
		{
			foreach (var attribute in current.GetAttributes())
			{
				if (
					attribute.AttributeClass is null
					|| attribute.AttributeClass.ToDisplayString()
						!= $"{TypeLibraryGenerator.ZodSharpCoreNamespace}.ZodRuleAttribute"
				)
				{
					continue;
				}

				if (attribute.ConstructorArguments.Length == 0)
					continue;

				if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol ruleType)
					continue;

				string? code = null;
				string? origin = null;
				foreach (var pair in attribute.NamedArguments)
				{
					switch (pair.Key)
					{
						case "Code":
							code = pair.Value.Value as string;
							break;
						case "Origin":
							origin = pair.Value.Value as string;
							break;
						default:
							break;
					}
				}

				mapping = new RuleMapping(ruleType, code, origin);
				return true;
			}
		}

		mapping = default;
		return false;
	}

	static bool ImplementsRuleFor(INamedTypeSymbol ruleType, ITypeSymbol propertyType)
	{
		// A rule that is still open (for example NotEmptyRule<T>) cannot be closed from an attribute alone.
		if (IsOpenGeneric(ruleType))
			return false;

		foreach (var iface in ruleType.AllInterfaces)
		{
			if (iface.TypeArguments.Length != 1)
				continue;

			var definition = iface.OriginalDefinition;
			if (definition.Name != "IValidationRule" || definition.Arity != 1)
				continue;

			if (definition.ContainingNamespace.ToDisplayString() != TypeLibraryGenerator.ZodSharpCoreNamespace)
				continue;

			if (TypeHelpers.IsSameType(iface.TypeArguments[0], propertyType))
				return true;
		}

		return false;
	}

	static bool TryBuildArguments(
		AttributeData attribute,
		INamedTypeSymbol ruleType,
		out EquatableArray<string> arguments,
		out string? unmappedParameterName
	)
	{
		arguments = new([]);
		unmappedParameterName = null;

		var constructor = ruleType
			.InstanceConstructors.Where(static c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
			.OrderByDescending(static c => c.Parameters.Length)
			.FirstOrDefault();

		if (constructor is null)
			return false;

		var validation = ValidationAttributeData.FromAttributeData(attribute);
		var positional = attribute.ConstructorArguments;
		Dictionary<string, TypedConstant> named = [with(StringComparer.OrdinalIgnoreCase)];
		foreach (var pair in attribute.NamedArguments)
			named[pair.Key] = pair.Value;

		var expressions = ImmutableArray.CreateBuilder<string>(constructor.Parameters.Length);

		for (var i = 0; i < constructor.Parameters.Length; i++)
		{
			var parameter = constructor.Parameters[i];

			if (named.TryGetValue(parameter.Name, out var namedValue))
			{
				if (!TryConvertConstant(namedValue, parameter.Type, out var namedExpression))
				{
					unmappedParameterName = parameter.Name;
					return false;
				}

				expressions.Add(namedExpression);
				continue;
			}

			if (i < positional.Length)
			{
				if (!TryConvertConstant(positional[i], parameter.Type, out var positionalExpression))
				{
					unmappedParameterName = parameter.Name;
					return false;
				}

				expressions.Add(positionalExpression);
				continue;
			}

			if (IsMessageParameter(parameter) && validation.Exists && !string.IsNullOrEmpty(validation.ErrorMessage))
			{
				expressions.Add(validation.ErrorMessage.StringLiteral());
				continue;
			}

			if (parameter.HasExplicitDefaultValue)
			{
				if (!TryConvertValue(parameter.ExplicitDefaultValue, parameter.Type, out var defaultExpression))
				{
					unmappedParameterName = parameter.Name;
					return false;
				}

				expressions.Add(parameter.ExplicitDefaultValue is null ? "null!" : defaultExpression);
				continue;
			}

			if (CanPassNull(parameter.Type))
			{
				expressions.Add("null");
				continue;
			}

			unmappedParameterName = parameter.Name;
			return false;
		}

		arguments = new(expressions.ToImmutable());
		return true;
	}

	/// <summary>
	/// Determines whether a bare <see langword="null"/> literal may be emitted for a value of
	/// <paramref name="type"/> without producing CS8625. A non-nullable reference type is excluded even though
	/// it can hold <see langword="null"/> at runtime, so such a value is reported as unmappable instead.
	/// </summary>
	static bool CanPassNull(ITypeSymbol type) =>
		type.NullableAnnotation == NullableAnnotation.Annotated
		|| type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }
		|| (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.None);

	static bool IsMessageParameter(IParameterSymbol parameter) =>
		string.Equals(parameter.Name, "message", StringComparison.OrdinalIgnoreCase);

	static bool TryConvertConstant(TypedConstant constant, ITypeSymbol targetType, out string expression)
	{
		if (constant.IsNull)
		{
			expression = "null";
			return CanPassNull(targetType);
		}

		if (constant.Kind == TypedConstantKind.Type)
		{
			if (constant.Value is ITypeSymbol typeSymbol)
			{
				expression = $"typeof({new TypeIdentity(typeSymbol).RenderFullName})";
				return true;
			}

			expression = string.Empty;
			return false;
		}

		return TryConvertValue(constant.Value, targetType, out expression);
	}

	internal static bool TryConvertValue(object? value, ITypeSymbol targetType, out string expression)
	{
		if (value is null)
		{
			expression = "null";
			return CanPassNull(targetType);
		}

		var unwrapped = TypeHelpers.UnwrapNullableType(targetType);

		if (unwrapped is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
		{
			expression =
				$"({new TypeIdentity(enumType).RenderFullName}){Convert.ToString(value, CultureInfo.InvariantCulture)}";
			return true;
		}

#pragma warning disable IDE0072 // Add missing cases
		expression = unwrapped.SpecialType switch
		{
			SpecialType.System_String when value is string text => text.StringLiteral(),
			SpecialType.System_Char when value is char character => CodeGenHelpers.QuoteChar(character),
			SpecialType.System_Boolean when value is bool boolean => boolean ? "true" : "false",
			SpecialType.System_Byte when value is byte number => number.ToString(CultureInfo.InvariantCulture),
			SpecialType.System_SByte when value is sbyte number =>
				$"(sbyte){number.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_Int16 when value is short number =>
				$"(short){number.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_UInt16 when value is ushort number =>
				$"(ushort){number.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_Int32 when value is int number => number.ToString(CultureInfo.InvariantCulture),
			SpecialType.System_UInt32 when value is uint number => $"{number.ToString(CultureInfo.InvariantCulture)}U",
			SpecialType.System_Int64 when value is long number => $"{number.ToString(CultureInfo.InvariantCulture)}L",
			SpecialType.System_UInt64 when value is ulong number =>
				$"{number.ToString(CultureInfo.InvariantCulture)}UL",
			SpecialType.System_Single when value is float number =>
				$"{number.ToString("R", CultureInfo.InvariantCulture)}F",
			SpecialType.System_Double when value is double number =>
				$"{number.ToString("R", CultureInfo.InvariantCulture)}D",
			SpecialType.System_Decimal when value is decimal number =>
				$"{number.ToString(CultureInfo.InvariantCulture)}M",
			SpecialType.System_Object when value is string text => text.StringLiteral(),
			_ => string.Empty,
		};
#pragma warning restore IDE0072 // Add missing cases

		return expression.Length > 0;
	}

	static Location GetAttributeLocation(AttributeData attributeData) =>
		attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;

	readonly record struct RuleMapping(INamedTypeSymbol RuleType, string? Code, string? Origin);
}
