using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Helpers.Models;
using ExecutionContext = ZodSharp.SourceGenerators.Helpers.Models.ExecutionContext;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void Execute(
		TargetSymbolDescriptor classInfo,
		ExecutionContext executionContext,
		SourceProductionContext context
	)
	{
		try
		{
			var source = GenerateSchemaClass(classInfo.Symbol, executionContext);
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

	static string GenerateSchemaClass(INamedTypeSymbol classSymbol, ExecutionContext executionContext)
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

		StringBuilder sb = new(estimatedCapacity);

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
		sb.AppendLine("{{CodeGen}}");
		sb.AppendLine($"public static class {schemaName}");
		sb.AppendLine("{");
		sb.AppendLine("    private static readonly string[] EmptyPath = global::System.Array.Empty<string>();");
		sb.AppendLine();

		GenerateValidateMethod(
			executionContext,
			sb,
			classSymbol,
			fullTypeName
		//, schemaName
		);
		GenerateParseMethod(sb, fullTypeName);

		sb.AppendLine("}");

		return sb.ToString();
	}

	static void GenerateValidateMethod(
		ExecutionContext executionContext,
		StringBuilder sb,
		INamedTypeSymbol classSymbol,
		string fullTypeName //,
	//string schemaName
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
			GeneratePropertyValidation(executionContext, sb, property, fullTypeName);
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

	static void GeneratePropertyValidation(
		ExecutionContext executionContext,
		StringBuilder sb,
		IPropertySymbol property,
		string _
	) //fullTypeName)
	{
		var propertyName = property.Name;
		var propertyType = property.Type;
		//var propertyTypeName = propertyType.ToDisplayString();
		var isNullable =
			propertyType.NullableAnnotation == NullableAnnotation.Annotated
			|| (propertyType.IsReferenceType && propertyType.NullableAnnotation != NullableAnnotation.NotAnnotated);

		var attributes = property.GetAttributes();
		var requiredAttrib = RequiredAttributeData.FromAttributeData(executionContext, attributes);
		if (!isNullable && propertyType.IsReferenceType)
			requiredAttrib = new(true, false, null);

		if (requiredAttrib.Exists)
		{
			var errorMessage = string.Format(
				CultureInfo.InvariantCulture,
				(requiredAttrib.ErrorMessage ?? "Required field '{0}' is null"),
				propertyName
			);

			sb.AppendLine($"        if (value.{propertyName} == null)");
			sb.AppendLine("        {");
			sb.AppendLine("            errors.Add(new ValidationError(");
			sb.AppendLine("                \"missing_field\",");
			sb.AppendLine($"                \"{errorMessage}\",");
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

		GenerateTypeSpecificValidations(executionContext, sb, property, propertyType, attributes, propertyName);

		sb.AppendLine("        }");
		sb.AppendLine();
	}

	static void GenerateTypeSpecificValidations(
		ExecutionContext executionContext,
		StringBuilder sb,
		IPropertySymbol _, //property,
		ITypeSymbol propertyType,
		ImmutableArray<AttributeData> attributes,
		string propertyName
	)
	{
		//var propertyTypeName = propertyType.ToDisplayString();

		if (propertyType.SpecialType == SpecialType.System_String)
		{
			GenerateStringValidations(executionContext, sb, propertyName, attributes);
		}
		else if (TypeHelpers.IsNumericType(propertyType))
		{
			GenerateNumericValidations(executionContext, sb, propertyName, propertyType, attributes);
		}
		else if (
			propertyType is INamedTypeSymbol namedType
			&& namedType.IsGenericType
			&& namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
		)
		{
			GenerateCollectionValidations(executionContext, sb, propertyName, attributes);
		}
	}

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

	static void GenerateCompositionMethods(
		StringBuilder sb,
		INamedTypeSymbol classSymbol,
		string fullTypeName //,
	//string schemaName
	)
	{
		sb.AppendLine();
		sb.AppendLine("    /// <summary>");
		sb.AppendLine("    /// Creates a schema that requires both this and another schema to pass.");
		sb.AppendLine("    /// Equivalent to Zod's .and() method.");
		sb.AppendLine("    /// </summary>");
		sb.AppendLine(
			$"    public static {TypeHelpers.ValidationResult.Global()}<{fullTypeName}> And({fullTypeName} value, global::System.Func<{fullTypeName}, bool> additionalValidation, string? message = null)"
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
		sb.AppendLine($"            return {TypeHelpers.ValidationResult.Global()}<{fullTypeName}>.Failure(");
		sb.AppendLine($"                new {TypeHelpers.ValidationError.Global()}(");
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
			$"    public static ${TypeHelpers.ValidationResult.Global()}<{fullTypeName}> Or({fullTypeName} value, global::System.Func<{fullTypeName}, bool> alternativeValidation, string? message = null)"
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
		sb.AppendLine($"            return {TypeHelpers.ValidationResult.Global()}<{fullTypeName}>.Success(value);");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine($"        return {TypeHelpers.ValidationResult.Global()}<{fullTypeName}>.Failure(");
		sb.AppendLine($"            new {TypeHelpers.ValidationError.Global()}(");
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
			$"    public static {TypeHelpers.ValidationResult.Global()}<{fullTypeName}> Refine({fullTypeName} value, global:System.Func<{fullTypeName}, bool> refinement, string? message = null)"
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
		sb.AppendLine($"            return {TypeHelpers.ValidationResult.Global()}<{fullTypeName}>.Failure(");
		sb.AppendLine($"            new {TypeHelpers.ValidationError.Global()}(");
		sb.AppendLine("                \"validation_failed\",");
		sb.AppendLine($"                message ?? \"Refinement validation failed for {classSymbol.Name}\",");
		sb.AppendLine("                EmptyPath");
		sb.AppendLine("            )");
		sb.AppendLine("            );");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine("        return result;");
		sb.AppendLine("    }");
	}
}
