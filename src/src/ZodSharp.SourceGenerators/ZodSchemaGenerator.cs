using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ZodSharp.SourceGenerators;

/// <summary>
/// Source generator that creates optimized validators for classes marked with [ZodSchema].
/// Uses IIncrementalGenerator for better performance and incremental compilation support.
/// </summary>
[Generator]
public sealed class ZodSchemaGenerator : IIncrementalGenerator
{
	const int EstimatedCodeSize = 2048; // Initial capacity for StringBuilder

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx =>
		{
			ctx.AddEmbeddedAttributeDefinition();
		});

		// Create a syntax provider that finds classes with [ZodSchema] attribute
		var classDeclarations = context
			.SyntaxProvider.CreateSyntaxProvider(
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
				transform: static (ctx, ct) => GetSemanticTargetForGeneration(ctx, ct)
			)
			.Where(static m => m is not null);

		// Register source output
		context.RegisterSourceOutput(classDeclarations, static (spc, source) => Execute(source!, spc));
	}

	/// <summary>
	/// Predicate to check if a syntax node is a candidate for generation.
	/// </summary>
	static bool IsSyntaxTargetForGeneration(SyntaxNode node)
	{
		return node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax;
	}

	/// <summary>
	/// Transform syntax node to semantic target if it has [ZodSchema] attribute.
	/// </summary>
	static ClassInfo? GetSemanticTargetForGeneration(
		GeneratorSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		var declaration = (TypeDeclarationSyntax)context.Node;

		var hasAttribute = declaration
			.AttributeLists.SelectMany(al => al.Attributes)
			.Any(attr =>
			{
				var name = attr.Name.ToString();
				return name == "ZodSchema" || name == "ZodSchemaAttribute";
			});

		if (!hasAttribute)
			return null;

		var semanticModel = context.SemanticModel;
		var symbol = semanticModel.GetDeclaredSymbol(declaration) as INamedTypeSymbol;

		if (symbol == null)
			return null;

		var attributeSymbol = semanticModel.Compilation.GetTypeByMetadataName(
			"ZodSharp.SourceGenerators.ZodSchemaAttribute"
		);
		if (attributeSymbol == null)
			return null;

		var hasZodSchemaAttribute = symbol
			.GetAttributes()
			.Any(attr => attr.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);

		if (!hasZodSchemaAttribute)
			return null;

		return new ClassInfo { Symbol = symbol, Declaration = declaration };
	}

	/// <summary>
	/// Generate source code for the schema class.
	/// </summary>
	static void Execute(ClassInfo classInfo, SourceProductionContext context)
	{
		try
		{
			var source = GenerateSchemaClass(classInfo.Symbol);
			var fileName = $"{classInfo.Symbol.Name}Schema.g.cs";
			context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
		}
		catch (Exception ex)
		{
			// Report diagnostic if generation fails
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(
					"ZODSGEN001",
					"Schema generation failed",
					"Failed to generate schema for {0}: {1}",
					"ZodSharp",
					DiagnosticSeverity.Error,
					isEnabledByDefault: true
				),
				classInfo.Declaration.GetLocation(),
				classInfo.Symbol.Name,
				ex.Message
			);

			context.ReportDiagnostic(diagnostic);
		}
	}

	/// <summary>
	/// Generates the schema class code.
	/// </summary>
	static string GenerateSchemaClass(INamedTypeSymbol classSymbol)
	{
		var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
		var className = classSymbol.Name;
		var schemaName = $"{className}Schema";
		var fullTypeName = classSymbol.ToDisplayString();

		// Estimate capacity based on number of properties
		var propertyCount = classSymbol
			.GetMembers()
			.OfType<IPropertySymbol>()
			.Count(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic);

		var estimatedCapacity = EstimatedCodeSize + (propertyCount * 200);

		var sb = new StringBuilder(estimatedCapacity);

		sb.AppendLine("using ZodSharp.Core;");
		sb.AppendLine("using System;");
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine("using System.Collections.Immutable;");
		sb.AppendLine();

		sb.AppendLine($"namespace {namespaceName};");
		sb.AppendLine();
		sb.AppendLine("/// <summary>");
		sb.AppendLine($"/// Auto-generated schema validator for {className}.");
		sb.AppendLine("/// This class is generated at compile time for zero-allocation validation.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine($"public static class {schemaName}");
		sb.AppendLine("{");
		sb.AppendLine("    private static readonly string[] EmptyPath = Array.Empty<string>();");
		sb.AppendLine();

		GenerateValidateMethod(sb, classSymbol, fullTypeName, schemaName);
		GenerateParseMethod(sb, fullTypeName);

		sb.AppendLine("}");

		return sb.ToString();
	}

	/// <summary>
	/// Generates the Validate method with comprehensive property validation.
	/// </summary>
	static void GenerateValidateMethod(
		StringBuilder sb,
		INamedTypeSymbol classSymbol,
		string fullTypeName,
		string schemaName
	)
	{
		sb.AppendLine("    /// <summary>");
		sb.AppendLine("    /// Validates an instance of the target type.");
		sb.AppendLine("    /// </summary>");
		sb.AppendLine($"    public static ValidationResult<{fullTypeName}> Validate({fullTypeName} value)");
		sb.AppendLine("    {");
		sb.AppendLine("        if (value == null)");
		sb.AppendLine("        {");
		sb.AppendLine($"            return ValidationResult<{fullTypeName}>.Failure(");
		sb.AppendLine("                new ValidationError(");
		sb.AppendLine("                    \"invalid_type\",");
		sb.AppendLine("                    \"Value cannot be null\",");
		sb.AppendLine("                    EmptyPath");
		sb.AppendLine("                )");
		sb.AppendLine("            );");
		sb.AppendLine("        }");
		sb.AppendLine();

		sb.AppendLine("        var errors = new List<ValidationError>();");
		sb.AppendLine();

		var properties = classSymbol
			.GetMembers()
			.OfType<IPropertySymbol>()
			.Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
			.ToList();

		foreach (var property in properties)
		{
			GeneratePropertyValidation(sb, property, fullTypeName);
		}

		sb.AppendLine();
		sb.AppendLine("        if (errors.Count > 0)");
		sb.AppendLine("        {");
		sb.AppendLine($"            return ValidationResult<{fullTypeName}>.Failure(errors);");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine($"        return ValidationResult<{fullTypeName}>.Success(value);");
		sb.AppendLine("    }");
	}

	/// <summary>
	/// Generates validation code for a property, including DataAnnotations attributes.
	/// </summary>
	static void GeneratePropertyValidation(StringBuilder sb, IPropertySymbol property, string fullTypeName)
	{
		var propertyName = property.Name;
		var propertyType = property.Type;
		var propertyTypeName = propertyType.ToDisplayString();
		var isNullable =
			propertyType.NullableAnnotation == NullableAnnotation.Annotated
			|| (propertyType.IsReferenceType && propertyType.NullableAnnotation != NullableAnnotation.NotAnnotated);

		var attributes = property.GetAttributes().ToList();

		var isRequired = attributes.Any(attr =>
			attr.AttributeClass?.Name == "RequiredAttribute"
			|| attr.AttributeClass?.Name.EndsWith("RequiredAttribute") == true
		);

		if (!isNullable && propertyType.IsReferenceType)
		{
			isRequired = true;
		}

		if (isRequired)
		{
			sb.AppendLine($"        if (value.{propertyName} == null)");
			sb.AppendLine("        {");
			sb.AppendLine("            errors.Add(new ValidationError(");
			sb.AppendLine("                \"missing_field\",");
			sb.AppendLine($"                \"Required field '{propertyName}' is null\",");
			sb.AppendLine($"                new[] {{ \"{propertyName}\" }}");
			sb.AppendLine("            ));");
			sb.AppendLine("        }");
			sb.AppendLine("        else");
			sb.AppendLine("        {");
		}
		else
		{
			sb.AppendLine($"        if (value.{propertyName} != null)");
			sb.AppendLine("        {");
		}

		GenerateTypeSpecificValidations(sb, property, propertyType, attributes, propertyName);

		sb.AppendLine("        }");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates type-specific validations based on property type and attributes.
	/// </summary>
	static void GenerateTypeSpecificValidations(
		StringBuilder sb,
		IPropertySymbol property,
		ITypeSymbol propertyType,
		List<AttributeData> attributes,
		string propertyName
	)
	{
		var propertyTypeName = propertyType.ToDisplayString();

		if (propertyType.SpecialType == SpecialType.System_String)
		{
			GenerateStringValidations(sb, propertyName, attributes);
		}
		else if (IsNumericType(propertyType))
		{
			GenerateNumericValidations(sb, propertyName, propertyType, attributes);
		}
		else if (
			propertyType is INamedTypeSymbol namedType
			&& namedType.IsGenericType
			&& namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
		)
		{
			GenerateCollectionValidations(sb, propertyName, attributes);
		}

		var hasEmailAttribute = attributes.Any(attr =>
			attr.AttributeClass?.Name == "EmailAddressAttribute"
			|| attr.AttributeClass?.Name.EndsWith("EmailAddressAttribute") == true
		);

		if (hasEmailAttribute && propertyType.SpecialType == SpecialType.System_String)
		{
			sb.AppendLine(
				$"            if (!System.Text.RegularExpressions.Regex.IsMatch(value.{propertyName}, @\"^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$\"))"
			);
			sb.AppendLine("            {");
			sb.AppendLine("                errors.Add(new ValidationError(");
			sb.AppendLine("                    \"invalid_string\",");
			sb.AppendLine($"                    \"Field '{propertyName}' must be a valid email address\",");
			sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
			sb.AppendLine("                ));");
			sb.AppendLine("            }");
		}
	}

	/// <summary>
	/// Generates string-specific validations.
	/// </summary>
	static void GenerateStringValidations(StringBuilder sb, string propertyName, List<AttributeData> attributes)
	{
		var stringLengthAttr = attributes.FirstOrDefault(attr =>
			attr.AttributeClass?.Name == "StringLengthAttribute"
			|| attr.AttributeClass?.Name.EndsWith("StringLengthAttribute") == true
		);

		if (stringLengthAttr != null)
		{
			var maxLength = stringLengthAttr.ConstructorArguments.FirstOrDefault().Value;
			var minLengthArg = stringLengthAttr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "MinimumLength");
			var minLength = minLengthArg.Value.Value ?? 0;

			if (minLength > 0)
			{
				sb.AppendLine($"            if (value.{propertyName}.Length < {minLength})");
				sb.AppendLine("            {");
				sb.AppendLine("                errors.Add(new ValidationError(");
				sb.AppendLine("                    \"too_small\",");
				sb.AppendLine(
					$"                    \"Field '{propertyName}' must be at least {minLength} characters long\","
				);
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}

			if (maxLength != null)
			{
				sb.AppendLine($"            if (value.{propertyName}.Length > {maxLength})");
				sb.AppendLine("            {");
				sb.AppendLine("                errors.Add(new ValidationError(");
				sb.AppendLine("                    \"too_big\",");
				sb.AppendLine(
					$"                    \"Field '{propertyName}' must be at most {maxLength} characters long\","
				);
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}
		}

		var minLengthAttr = attributes.FirstOrDefault(attr =>
			attr.AttributeClass?.Name == "MinLengthAttribute"
			|| attr.AttributeClass?.Name.EndsWith("MinLengthAttribute") == true
		);

		if (minLengthAttr != null)
		{
			var minLength = minLengthAttr.ConstructorArguments.FirstOrDefault().Value;
			if (minLength != null)
			{
				sb.AppendLine($"            if (value.{propertyName}.Length < {minLength})");
				sb.AppendLine("            {");
				sb.AppendLine("                errors.Add(new ValidationError(");
				sb.AppendLine("                    \"too_small\",");
				sb.AppendLine(
					$"                    \"Field '{propertyName}' must be at least {minLength} characters long\","
				);
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}
		}

		var maxLengthAttr = attributes.FirstOrDefault(attr =>
			attr.AttributeClass?.Name == "MaxLengthAttribute"
			|| attr.AttributeClass?.Name.EndsWith("MaxLengthAttribute") == true
		);

		if (maxLengthAttr != null)
		{
			var maxLength = maxLengthAttr.ConstructorArguments.FirstOrDefault().Value;
			if (maxLength != null)
			{
				sb.AppendLine($"            if (value.{propertyName}.Length > {maxLength})");
				sb.AppendLine("            {");
				sb.AppendLine("                errors.Add(new ValidationError(");
				sb.AppendLine("                    \"too_big\",");
				sb.AppendLine(
					$"                    \"Field '{propertyName}' must be at most {maxLength} characters long\","
				);
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}
		}
	}

	/// <summary>
	/// Generates numeric-specific validations.
	/// </summary>
	static void GenerateNumericValidations(
		StringBuilder sb,
		string propertyName,
		ITypeSymbol propertyType,
		List<AttributeData> attributes
	)
	{
		var rangeAttr = attributes.FirstOrDefault(attr =>
			attr.AttributeClass?.Name == "RangeAttribute"
			|| attr.AttributeClass?.Name.EndsWith("RangeAttribute") == true
		);

		if (rangeAttr != null)
		{
			var args = rangeAttr.ConstructorArguments;
			if (args.Length >= 2)
			{
				var min = args[0].Value;
				var max = args[1].Value;

				sb.AppendLine($"            if (value.{propertyName} < {min} || value.{propertyName} > {max})");
				sb.AppendLine("            {");
				sb.AppendLine("                errors.Add(new ValidationError(");
				sb.AppendLine("                    \"invalid_number\",");
				sb.AppendLine($"                    \"Field '{propertyName}' must be between {min} and {max}\",");
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}
		}
	}

	/// <summary>
	/// Generates collection-specific validations.
	/// </summary>
	static void GenerateCollectionValidations(StringBuilder sb, string propertyName, List<AttributeData> attributes)
	{
		var minLengthAttr = attributes.FirstOrDefault(attr =>
			attr.AttributeClass?.Name == "MinLengthAttribute"
			|| attr.AttributeClass?.Name.EndsWith("MinLengthAttribute") == true
		);

		if (minLengthAttr != null)
		{
			var minLength = minLengthAttr.ConstructorArguments.FirstOrDefault().Value;
			if (minLength != null)
			{
				sb.AppendLine($"            if (System.Linq.Enumerable.Count(value.{propertyName}) < {minLength})");
				sb.AppendLine("            {");
				sb.AppendLine("                errors.Add(new ValidationError(");
				sb.AppendLine("                    \"too_small\",");
				sb.AppendLine($"                    \"Field '{propertyName}' must have at least {minLength} items\",");
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}
		}

		var maxLengthAttr = attributes.FirstOrDefault(attr =>
			attr.AttributeClass?.Name == "MaxLengthAttribute"
			|| attr.AttributeClass?.Name.EndsWith("MaxLengthAttribute") == true
		);

		if (maxLengthAttr != null)
		{
			var maxLength = maxLengthAttr.ConstructorArguments.FirstOrDefault().Value;
			if (maxLength != null)
			{
				sb.AppendLine($"            if (System.Linq.Enumerable.Count(value.{propertyName}) > {maxLength})");
				sb.AppendLine("            {");
				sb.AppendLine("                errors.Add(new ValidationError(");
				sb.AppendLine("                    \"too_big\",");
				sb.AppendLine($"                    \"Field '{propertyName}' must have at most {maxLength} items\",");
				sb.AppendLine($"                    new[] {{ \"{propertyName}\" }}");
				sb.AppendLine("                ));");
				sb.AppendLine("            }");
			}
		}
	}

	/// <summary>
	/// Generates the Parse method.
	/// </summary>
	static void GenerateParseMethod(StringBuilder sb, string fullTypeName)
	{
		sb.AppendLine("    /// <summary>");
		sb.AppendLine("    /// Validates and returns the value, throwing an exception if validation fails.");
		sb.AppendLine("    /// </summary>");
		sb.AppendLine($"    public static {fullTypeName} Parse({fullTypeName} value)");
		sb.AppendLine("    {");
		sb.AppendLine("        return Validate(value).GetValueOrThrow();");
		sb.AppendLine("    }");
	}

	/// <summary>
	/// Generates composition methods (.and(), .or(), .refine()).
	/// </summary>
	static void GenerateCompositionMethods(
		StringBuilder sb,
		INamedTypeSymbol classSymbol,
		string fullTypeName,
		string schemaName
	)
	{
		sb.AppendLine();
		sb.AppendLine("    /// <summary>");
		sb.AppendLine("    /// Creates a schema that requires both this and another schema to pass.");
		sb.AppendLine("    /// Equivalent to Zod's .and() method.");
		sb.AppendLine("    /// </summary>");
		sb.AppendLine(
			$"    public static ValidationResult<{fullTypeName}> And({fullTypeName} value, Func<{fullTypeName}, bool> additionalValidation, string? message = null)"
		);
		sb.AppendLine("    {");
		sb.AppendLine("        var result = Validate(value);");
		sb.AppendLine("        if (!result.IsSuccess)");
		sb.AppendLine("        {");
		sb.AppendLine("            return result;");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine("        if (!additionalValidation(value))");
		sb.AppendLine("        {");
		sb.AppendLine("            return ValidationResult<" + fullTypeName + ">.Failure(");
		sb.AppendLine("                new ValidationError(");
		sb.AppendLine("                    \"validation_failed\",");
		sb.AppendLine($"                    message ?? \"Additional validation failed for {classSymbol.Name}\",");
		sb.AppendLine("                    EmptyPath");
		sb.AppendLine("                )");
		sb.AppendLine("            );");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine("        return result;");
		sb.AppendLine("    }");
		sb.AppendLine();

		sb.AppendLine("    /// <summary>");
		sb.AppendLine("    /// Creates a schema that requires either this or another validation to pass.");
		sb.AppendLine("    /// Equivalent to Zod's .or() method.");
		sb.AppendLine("    /// </summary>");
		sb.AppendLine(
			$"    public static ValidationResult<{fullTypeName}> Or({fullTypeName} value, Func<{fullTypeName}, bool> alternativeValidation, string? message = null)"
		);
		sb.AppendLine("    {");
		sb.AppendLine("        var result = Validate(value);");
		sb.AppendLine("        if (result.IsSuccess)");
		sb.AppendLine("        {");
		sb.AppendLine("            return result;");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine("        if (alternativeValidation(value))");
		sb.AppendLine("        {");
		sb.AppendLine($"            return ValidationResult<{fullTypeName}>.Success(value);");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine("        return ValidationResult<" + fullTypeName + ">.Failure(");
		sb.AppendLine("            new ValidationError(");
		sb.AppendLine("                \"validation_failed\",");
		sb.AppendLine($"                message ?? \"Neither validation passed for {classSymbol.Name}\",");
		sb.AppendLine("                EmptyPath");
		sb.AppendLine("            )");
		sb.AppendLine("        );");
		sb.AppendLine("    }");
		sb.AppendLine();

		sb.AppendLine("    /// <summary>");
		sb.AppendLine("    /// Adds a custom refinement to the validation.");
		sb.AppendLine("    /// Equivalent to Zod's .refine() method.");
		sb.AppendLine("    /// </summary>");
		sb.AppendLine(
			$"    public static ValidationResult<{fullTypeName}> Refine({fullTypeName} value, Func<{fullTypeName}, bool> refinement, string? message = null)"
		);
		sb.AppendLine("    {");
		sb.AppendLine("        var result = Validate(value);");
		sb.AppendLine("        if (!result.IsSuccess)");
		sb.AppendLine("        {");
		sb.AppendLine("            return result;");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine("        if (!refinement(value))");
		sb.AppendLine("        {");
		sb.AppendLine("            return ValidationResult<" + fullTypeName + ">.Failure(");
		sb.AppendLine("                new ValidationError(");
		sb.AppendLine("                    \"validation_failed\",");
		sb.AppendLine($"                    message ?? \"Refinement validation failed for {classSymbol.Name}\",");
		sb.AppendLine("                    EmptyPath");
		sb.AppendLine("                )");
		sb.AppendLine("            );");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine("        return result;");
		sb.AppendLine("    }");
	}

	/// <summary>
	/// Checks if a type is numeric.
	/// </summary>
	static bool IsNumericType(ITypeSymbol type)
	{
		return type.SpecialType
			is SpecialType.System_Byte
				or SpecialType.System_SByte
				or SpecialType.System_Int16
				or SpecialType.System_UInt16
				or SpecialType.System_Int32
				or SpecialType.System_UInt32
				or SpecialType.System_Int64
				or SpecialType.System_UInt64
				or SpecialType.System_Single
				or SpecialType.System_Double
				or SpecialType.System_Decimal;
	}

	/// <summary>
	/// Information about a class to generate schema for.
	/// </summary>
	sealed class ClassInfo
	{
		public required INamedTypeSymbol Symbol { get; init; }
		public required TypeDeclarationSyntax Declaration { get; init; }
	}
}
