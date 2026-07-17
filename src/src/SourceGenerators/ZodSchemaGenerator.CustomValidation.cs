using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	const string CustomValidationMethodNameProperty = "CustomValidationMethodName";

	/// <summary>
	/// Finds the <c>[ZodSchema]</c> attribute on the given class symbol.
	/// </summary>
	static AttributeData? GetZodSchemaAttribute(INamedTypeSymbol classSymbol, GenerationContext generationContext)
	{
		if (generationContext.ZodSchemaAttribute is null)
			return null;

		var comparer = SymbolEqualityComparer.Default;
		foreach (var attr in classSymbol.GetAttributes())
		{
			if (comparer.Equals(attr.AttributeClass, generationContext.ZodSchemaAttribute))
				return attr;
		}

		// Fallback: match by metadata name if symbol equality fails (e.g. embedded attribute).
		foreach (var attr in classSymbol.GetAttributes())
		{
			if (attr.AttributeClass?.ToDisplayString() == TypeHelpers.ZodSchemaAttribute)
				return attr;
		}

		return null;
	}

	/// <summary>
	/// Reads the <c>CustomValidationMethodName</c> property from the <c>[ZodSchema]</c> attribute,
	/// discovers and validates the matching method on the schema type, and returns
	/// an immutable <see cref="CustomValidationMethodData"/>.
	/// </summary>
	static CustomValidationMethodData ResolveCustomValidationMethod(
		INamedTypeSymbol classSymbol,
		AttributeData? zodSchemaAttribute,
		GenerationContext generationContext
	)
	{
		var configuredName = zodSchemaAttribute is not null ? GetCustomValidationMethodName(zodSchemaAttribute) : null;
		var isExplicitlyConfigured = !string.IsNullOrEmpty(configuredName);

		var methodName = string.IsNullOrWhiteSpace(configuredName)
			? TypeHelpers.DefaultCustomValidationMethodName
			: configuredName!;

		// Validate the configured name is a valid C# identifier.
		if (isExplicitlyConfigured && !IsValidIdentifier(methodName))
		{
			return new(
				IsConfigured: true,
				Exists: false,
				IsValid: false,
				MethodName: methodName,
				InvocationKind: CustomValidationInvocationKind.None,
				Diagnostics:
				[
					DiagnosticInfo.Create(
						GeneratorDiagnostics.CustomValidationInvalidMethodName,
						GetAttributeLocation(zodSchemaAttribute, classSymbol),
						methodName,
						classSymbol.Name
					),
				]
			);
		}

		// Discover candidate methods declared directly on the type (not inherited).
		var candidates = GetMethodsByName(classSymbol, methodName);

		generationContext.Logger?.Info(
			$"CustomValidation: method='{methodName}', configured={isExplicitlyConfigured}, candidates={candidates.Count}",
			1
		);

		var validationMethodClassSymbol = classSymbol;
		if (candidates.Count == 0)
		{
			var (s, c) = FindCandidatesFromSchemaValidator(classSymbol, methodName);
			validationMethodClassSymbol = s;

			if (c.Count == 0)
			{
				// No method found, checking the model and the schema validator...but only report a diagnostic if explicitly configured.
				if (isExplicitlyConfigured)
				{
					return new CustomValidationMethodData(
						IsConfigured: true,
						Exists: false,
						IsValid: false,
						MethodName: methodName,
						InvocationKind: CustomValidationInvocationKind.None,
						Diagnostics:
						[
							DiagnosticInfo.Create(
								GeneratorDiagnostics.CustomValidationMethodNotFound,
								GetAttributeLocation(zodSchemaAttribute, classSymbol),
								methodName,
								classSymbol.Name
							),
						]
					);
				}
			}

			if (validationMethodClassSymbol == null)
			{
				// We checked the methods on the model and on the schema validator...
				return CustomValidationMethodData.None;
			}

			// Set the current candidates to the one(s) we found on the schema.
			candidates = c;
		}

		var invocationKind = SymbolEqualityComparer.Default.Equals(validationMethodClassSymbol, classSymbol)
			? CustomValidationInvocationKind.StaticOnModelType
			: CustomValidationInvocationKind.DefinedOnSchemaValidator;

		// Validate each candidate and collect valid ones + diagnostics.
		var validCandidates = new List<IMethodSymbol>();
		var diagnostics = new List<DiagnosticInfo>();

		foreach (var candidate in candidates)
		{
			var (isValid, candidateDiagnostics) = ValidateMethodSignature(
				candidate,
				validationMethodClassSymbol,
				classSymbol,
				generationContext,
				invocationKind
			);
			diagnostics.AddRange(candidateDiagnostics);
			if (isValid)
				validCandidates.Add(candidate);
		}

		if (validCandidates.Count == 0)
		{
			return new CustomValidationMethodData(
				IsConfigured: true,
				Exists: true,
				IsValid: false,
				MethodName: methodName,
				InvocationKind: CustomValidationInvocationKind.None,
				Diagnostics: [.. diagnostics]
			);
		}

		if (validCandidates.Count > 1)
		{
			// Ambiguous — multiple valid overloads.
			return new CustomValidationMethodData(
				IsConfigured: true,
				Exists: true,
				IsValid: false,
				MethodName: methodName,
				InvocationKind: CustomValidationInvocationKind.None,
				Diagnostics:
				[
					DiagnosticInfo.Create(
						GeneratorDiagnostics.CustomValidationAmbiguousOverloads,
						validCandidates[0].Locations.Length > 0 ? validCandidates[0].Locations[0] : null,
						methodName,
						classSymbol.Name
					),
				]
			);
		}

		// Exactly one valid method.
		return new CustomValidationMethodData(
			IsConfigured: true,
			Exists: true,
			IsValid: true,
			MethodName: methodName,
			InvocationKind: invocationKind,
			Diagnostics: []
		);
	}

	static (INamedTypeSymbol?, List<IMethodSymbol>) FindCandidatesFromSchemaValidator(
		INamedTypeSymbol classSymbol,
		string validationMethodName
	)
	{
		var schemaSymbol = classSymbol
			.ContainingNamespace.GetTypeMembers($"{classSymbol.Name}SchemaValidator")
			.FirstOrDefault();
		if (schemaSymbol == null)
			return (null, []);

		return (
			schemaSymbol,
			[
				.. schemaSymbol
					.GetMembers(validationMethodName)
					.Where(static m => m is IMethodSymbol)
					.Cast<IMethodSymbol>(),
			]
		);
	}

	static string? GetCustomValidationMethodName(AttributeData attributeData)
	{
		foreach (var namedArg in attributeData.NamedArguments)
		{
			if (namedArg.Key == CustomValidationMethodNameProperty && namedArg.Value.Value is string name)
				return name;
		}

		return null;
	}

	static Location? GetAttributeLocation(AttributeData? attributeData, INamedTypeSymbol classSymbol)
	{
		if (attributeData?.ApplicationSyntaxReference?.GetSyntax() is { } syntax)
			return syntax.GetLocation();
		return classSymbol.Locations.Length > 0 ? classSymbol.Locations[0] : null;
	}

	static List<IMethodSymbol> GetMethodsByName(INamedTypeSymbol classSymbol, string methodName) =>
		[
			.. classSymbol
				.GetMembers(methodName)
				.OfType<IMethodSymbol>()
				.Where(static m => m.MethodKind == MethodKind.Ordinary && !m.IsImplicitlyDeclared),
		];

	static (bool IsValid, List<DiagnosticInfo> Diagnostics) ValidateMethodSignature(
		IMethodSymbol method,
		INamedTypeSymbol validationClassSymbol,
		INamedTypeSymbol classSymbol,
		GenerationContext generationContext,
		CustomValidationInvocationKind invocationKind
	)
	{
		var diagnostics = new List<DiagnosticInfo>();
		var typeName = validationClassSymbol.Name;
		var methodLocation = method.Locations.Length > 0 ? method.Locations[0] : null;
		var comparer = SymbolEqualityComparer.Default;

		// Must not be generic.
		if (method.IsGenericMethod)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.CustomValidationGenericMethod,
					methodLocation,
					method.Name,
					typeName
				)
			);
		}

		// Must not be abstract.
		if (method.IsAbstract)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.CustomValidationAbstractMethod,
					methodLocation,
					method.Name,
					typeName
				)
			);
		}

		// Must not be an unimplemented partial method.
		if (method.PartialDefinitionPart is not null && method.PartialImplementationPart is null)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.CustomValidationUnimplementedPartial,
					methodLocation,
					method.Name,
					typeName
				)
			);
		}
		else if (method.IsPartialDefinition && method.PartialImplementationPart is null)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.CustomValidationUnimplementedPartial,
					methodLocation,
					method.Name,
					typeName
				)
			);
		}

		// Must be static — the generated validator is a separate type, not a partial of the model.
		if (invocationKind == CustomValidationInvocationKind.StaticOnModelType && !method.IsStatic)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.CustomValidationInvalidStaticInstance,
					methodLocation,
					method.Name,
					typeName
				)
			);
		}

		// Check parameter modifiers (ref, in, out, params, scoped).
		foreach (var param in method.Parameters)
		{
			if (
				param.RefKind is RefKind.Ref or RefKind.In or RefKind.Out
				|| param.IsParams
				|| param.ScopedKind != ScopedKind.None
			)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						GeneratorDiagnostics.CustomValidationInvalidParameterModifier,
						methodLocation,
						method.Name,
						typeName
					)
				);
				break;
			}
		}

		// Must have exactly two parameters.
		if (method.Parameters.Length != 2)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.CustomValidationInvalidParameterCount,
					methodLocation,
					method.Name,
					typeName
				)
			);

			// Can't validate individual params if count is wrong; return early.
			return (false, diagnostics);
		}

		// First parameter must be the schema model type.
		var firstParam = method.Parameters[0];
		if (!TypeHelpers.IsSameType(firstParam.Type, classSymbol, comparer))
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.CustomValidationInvalidModelParameter,
					methodLocation,
					method.Name,
					typeName
				)
			);
		}

		// Second parameter must be CancellationToken.
		var secondParam = method.Parameters[1];
		if (
			generationContext.CancellationToken is not null
			&& !comparer.Equals(TypeHelpers.UnwrapNullableType(secondParam.Type), generationContext.CancellationToken)
		)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.CustomValidationInvalidCancellationToken,
					methodLocation,
					method.Name,
					typeName
				)
			);
		}

		// Return type must be ValueTask<ValidationResult<T>>.
		var expectedReturnType = GetExpectedReturnType(validationClassSymbol, generationContext);
		if (expectedReturnType is not null && !comparer.Equals(method.ReturnType, expectedReturnType))
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.CustomValidationInvalidReturnType,
					methodLocation,
					method.Name,
					typeName
				)
			);
		}

		if (invocationKind == CustomValidationInvocationKind.StaticOnModelType)
		{
			// Accessibility check — the generated validator is in the same assembly + namespace,
			// so internal and public are accessible. Private is not.
			if (method.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						GeneratorDiagnostics.CustomValidationInaccessible,
						methodLocation,
						method.Name,
						typeName
					)
				);
			}
		}

		return (diagnostics.Count == 0, diagnostics);
	}

	/// <summary>
	/// Constructs the expected <c>ValueTask&lt;ValidationResult&lt;T&gt;&gt;</c> symbol
	/// from the compilation's framework symbols.
	/// </summary>
	static INamedTypeSymbol? GetExpectedReturnType(INamedTypeSymbol classSymbol, GenerationContext generationContext)
	{
		if (generationContext.ValueTaskOfT is null || generationContext.ValidationResult is null)
			return null;

		var validationResultOfT = generationContext.ValidationResult.Construct(classSymbol);
		return generationContext.ValueTaskOfT.Construct(validationResultOfT);
	}

	static bool IsValidIdentifier(string name) =>
		!string.IsNullOrWhiteSpace(name) && SyntaxFacts.IsValidIdentifier(name);
}
