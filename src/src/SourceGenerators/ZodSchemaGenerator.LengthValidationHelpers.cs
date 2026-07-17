using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	readonly record struct LengthAccessor(string LengthExpression, string Origin, bool IsSupported);

	static AttributeData? FindAttribute(ImmutableArray<AttributeData> attributes, INamedTypeSymbol? attributeSymbol)
	{
		if (attributeSymbol is null)
			return null;

		for (var i = 0; i < attributes.Length; i++)
		{
			if (SymbolEqualityComparer.Default.Equals(attributes[i].AttributeClass, attributeSymbol))
				return attributes[i];
		}

		return null;
	}

	static string GetDisplayName(GenerationContext generationContext, IPropertySymbol property)
	{
		var displayAttribute = FindAttribute(property.GetAttributes(), generationContext.DisplayAttribute);
		if (displayAttribute is not null)
		{
			foreach (var namedArgument in displayAttribute.NamedArguments)
			{
				if (
					namedArgument.Key == "Name"
					&& namedArgument.Value.Value is string name
					&& !string.IsNullOrEmpty(name)
				)
					return name;
			}
		}

		return property.Name;
	}

	static LengthAccessor ClassifyLengthAccessor(GenerationContext generationContext, ITypeSymbol propertyType)
	{
		if (propertyType.SpecialType == SpecialType.System_String || propertyType is IArrayTypeSymbol)
			return new("propertyValue.Length", propertyType is IArrayTypeSymbol ? "array" : "string", true);

		if (TypeHelpers.HasAccessibleCountProperty(propertyType))
			return new("propertyValue.Count", "collection", true);

		if (TypeHelpers.ImplementsInterface(propertyType, generationContext.IEnumerableOfT))
			return new(
				"global::ZodSharp.Optimizations.CollectionCountHelper.GetCount(propertyValue)",
				"collection",
				true
			);

		if (TypeHelpers.ImplementsInterface(propertyType, generationContext.IEnumerable))
			return new(
				"global::ZodSharp.Optimizations.CollectionCountHelper.GetCount(propertyValue)",
				"collection",
				true
			);

		// ...just a normal collection?
		return new(string.Empty, "collection", false);
	}

	static Location? GetAttributeLocation(AttributeData? attributeData) =>
		attributeData?.ApplicationSyntaxReference?.GetSyntax().GetLocation();

	static void AddInvalidLengthConfigurationDiagnostic(
		List<DiagnosticInfo> diagnostics,
		AttributeData? attributeData,
		string message
	) =>
		diagnostics.Add(
			DiagnosticInfo.Create(
				GeneratorDiagnostics.InvalidLengthAttribute,
				GetAttributeLocation(attributeData),
				message
			)
		);

	static void AddUnsupportedLengthTargetDiagnostic(
		List<DiagnosticInfo> diagnostics,
		AttributeData? attributeData,
		string propertyName,
		ITypeSymbol propertyType
	) =>
		diagnostics.Add(
			DiagnosticInfo.Create(
				GeneratorDiagnostics.UnsupportedLengthAttributeTarget,
				GetAttributeLocation(attributeData),
				string.Format(
					CultureInfo.InvariantCulture,
					"LengthAttribute cannot be applied to '{0}' because '{1}' exposes no accessible Length or Count member and is not an enumerable shape that ZodSharp can count safely.",
					propertyName,
					propertyType.ToDisplayString()
				)
			)
		);

	static void AddUnsupportedDataAnnotationsDiagnostic(
		List<DiagnosticInfo> diagnostics,
		AttributeData? attributeData,
		string message
	) =>
		diagnostics.Add(
			DiagnosticInfo.Create(
				GeneratorDiagnostics.UnsupportedDataAnnoationsUsage,
				GetAttributeLocation(attributeData),
				message
			)
		);

	static string BuildMessageExpression(
		List<DiagnosticInfo> diagnostics,
		AttributeData? attributeData,
		string displayName,
		string? literalMessage,
		string? resourceName,
		INamedTypeSymbol? resourceType,
		string defaultMessageExpression,
		params string[] formatArguments
	)
	{
		if (!string.IsNullOrEmpty(resourceName) || resourceType is not null)
		{
			if (string.IsNullOrEmpty(resourceName) || resourceType is null)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						GeneratorDiagnostics.InvalidDataAnnotationsErrorMessage,
						GetAttributeLocation(attributeData),
						"ErrorMessageResourceName and ErrorMessageResourceType must both be supplied."
					)
				);
				return defaultMessageExpression;
			}

			var resourceProperty = resourceType
				.GetMembers(resourceName!)
				.OfType<IPropertySymbol>()
				.FirstOrDefault(p =>
					p.IsStatic
					&& p.Parameters.Length == 0
					&& p.Type.SpecialType == SpecialType.System_String
					&& p.GetMethod is not null
					&& p.GetMethod.DeclaredAccessibility == Accessibility.Public
				);

			if (resourceProperty is null)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						GeneratorDiagnostics.InvalidDataAnnotationsErrorMessage,
						GetAttributeLocation(attributeData),
						string.Format(
							CultureInfo.InvariantCulture,
							"Unable to resolve a public static string resource property '{0}' on '{1}'.",
							resourceName,
							resourceType.ToDisplayString()
						)
					)
				);
				return defaultMessageExpression;
			}

			return BuildFormatExpression(
				$"{resourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{resourceProperty.Name}",
				formatArguments
			);
		}

		return !string.IsNullOrEmpty(literalMessage)
			? BuildFormatExpression(Quote(literalMessage!), formatArguments)
			: defaultMessageExpression;
	}

	static string BuildFormatExpression(string formatExpression, params string[] formatArguments) =>
		formatArguments.Length == 0
			? formatExpression
			: $"global::System.String.Format(global::System.Globalization.CultureInfo.CurrentCulture, {formatExpression}, {string.Join(", ", formatArguments)})";

	static void WriteValidationError(
		GenerationContext generationContext,
		string errorCode,
		string messageExpression,
		string pathFieldName,
		string origin,
		int? minimum = null,
		int? maximum = null
	)
	{
		generationContext.Writer.WriteLine(
			"errors ??= new global::System.Collections.Generic.List<global::ZodSharp.Core.ValidationError>();"
		);
		generationContext.Writer.WriteLine(
			$"errors.Add({TypeHelpers.ValidationError.Global()}.Create({Quote(errorCode)}, {messageExpression}, {pathFieldName}, origin: {Quote(origin)}, minimum: {(minimum.HasValue ? minimum.Value.ToString(CultureInfo.InvariantCulture) : "null")}, maximum: {(maximum.HasValue ? maximum.Value.ToString(CultureInfo.InvariantCulture) : "null")}, inclusive: true));"
		);
	}

	static void WriteValidationError(
		GenerationContext generationContext,
		string errorCode,
		string messageExpression,
		string pathFieldName
	)
	{
		generationContext.Writer.WriteLine(
			"errors ??= new global::System.Collections.Generic.List<global::ZodSharp.Core.ValidationError>();"
		);
		generationContext.Writer.WriteLine(
			$"errors.Add({TypeHelpers.ValidationError.Global()}.Create({Quote(errorCode)}, {messageExpression}, {pathFieldName}));"
		);
	}

	static string GetRegexFieldName(string propertyName) => $"Regex_{propertyName}";

	static string GetRangeMinimumFieldName(string propertyName) => $"RangeMinimum_{propertyName}";

	static string GetRangeMaximumFieldName(string propertyName) => $"RangeMaximum_{propertyName}";

	static string GetFullyQualifiedTypeName(ITypeSymbol type) =>
		TypeHelpers.UnwrapNullableType(type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

	static bool TryBuildTypedConstantExpression(
		TypedConstant constant,
		ITypeSymbol propertyType,
		bool propertyCanBeNull,
		out string expression,
		out string displayValue
	)
	{
		var targetType = TypeHelpers.UnwrapNullableType(propertyType);
		displayValue = constant.IsNull ? "null" : constant.Value?.ToString() ?? "null";

		if (constant.IsNull)
		{
			if (!propertyCanBeNull)
			{
				expression = string.Empty;
				return false;
			}

			expression = "null";
			return true;
		}

		if (targetType.TypeKind == TypeKind.Enum && constant.Value is not null)
		{
			expression =
				$"({GetFullyQualifiedTypeName(targetType)}){Convert.ToString(constant.Value, CultureInfo.InvariantCulture)}";
			return true;
		}

		expression = targetType.SpecialType switch
		{
			SpecialType.System_String when constant.Value is string value => Quote(value),
			SpecialType.System_Char when constant.Value is char value => QuoteChar(value),
			SpecialType.System_Boolean when constant.Value is bool value => value ? "true" : "false",
			SpecialType.System_Byte when constant.Value is byte value => value.ToString(CultureInfo.InvariantCulture),
			SpecialType.System_SByte when constant.Value is sbyte value =>
				$"(sbyte){value.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_Int16 when constant.Value is short value =>
				$"(short){value.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_UInt16 when constant.Value is ushort value =>
				$"(ushort){value.ToString(CultureInfo.InvariantCulture)}",
			SpecialType.System_Int32 when constant.Value is int value => value.ToString(CultureInfo.InvariantCulture),
			SpecialType.System_UInt32 when constant.Value is uint value =>
				$"{value.ToString(CultureInfo.InvariantCulture)}U",
			SpecialType.System_Int64 when constant.Value is long value =>
				$"{value.ToString(CultureInfo.InvariantCulture)}L",
			SpecialType.System_UInt64 when constant.Value is ulong value =>
				$"{value.ToString(CultureInfo.InvariantCulture)}UL",
			SpecialType.System_Single when constant.Value is float value => value.ToString(
				"R",
				CultureInfo.InvariantCulture
			) + "F",
			SpecialType.System_Double when constant.Value is double value => value.ToString(
				"R",
				CultureInfo.InvariantCulture
			) + "D",
			SpecialType.System_Decimal when constant.Value is decimal value => value.ToString(
				CultureInfo.InvariantCulture
			) + "M",
			_ => string.Empty,
		};

		return expression.Length > 0;
	}

	static string QuoteChar(char value)
	{
		return value switch
		{
			'\'' => "'\\''",
			'\\' => "'\\\\'",
			'\0' => "'\\0'",
			'\a' => "'\\a'",
			'\b' => "'\\b'",
			'\f' => "'\\f'",
			'\n' => "'\\n'",
			'\r' => "'\\r'",
			'\t' => "'\\t'",
			'\v' => "'\\v'",
			_ => $"'{value}'",
		};
	}

	static string BuildEqualityComparisonExpression(
		ITypeSymbol propertyType,
		string propertyValueExpression,
		string constantExpression
	)
	{
		return $"global::System.Collections.Generic.EqualityComparer<{propertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Default.Equals({propertyValueExpression}, {constantExpression})";
	}

	static string BuildValueListDisplay(ImmutableArray<TypedConstant> values)
	{
		if (values.IsDefaultOrEmpty)
			return string.Empty;

		var parts = new List<string>(values.Length);
		for (var i = 0; i < values.Length; i++)
		{
			var value = values[i];
			if (value.IsNull)
			{
				parts.Add("null");
				continue;
			}

			parts.Add(
				value.Value is string text
					? text
					: Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty
			);
		}

		return string.Join(", ", parts);
	}
}
