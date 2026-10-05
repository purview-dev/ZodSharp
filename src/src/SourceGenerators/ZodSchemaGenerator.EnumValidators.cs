using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;
using ZodSharp.SourceGenerators.Models.DataAttributes;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	/// <summary>
	/// Determines whether the automatic enum validation is emitted for the property. A flags combination is a
	/// valid value without being a defined member, and an explicit allow-list governs the property itself, so
	/// neither is validated by the enum rule.
	/// </summary>
	/// <param name="property">The property to validate.</param>
	/// <param name="validateEnumValues">Whether the schema asked for the automatic enum validation.</param>
	/// <returns><see langword="true"/> when the enum rule is emitted.</returns>
	static bool IsEnumValidationEmitted(ZodPropertyDescriptor property, bool validateEnumValues)
	{
		if (!validateEnumValues || !property.IsEnum || property.IsFlagsEnum)
			return false;

		var allowedValues = property.ValidationAttributes.AllowedValues;
		return !(allowedValues.ShouldProcess && allowedValues.Value.Exists);
	}

	/// <summary>
	/// Emits the automatic enum validation: an <c>EnumRule&lt;TEnum&gt;</c> instance the property value must
	/// satisfy. The rule owns the reported error identity, so a failure surfaces as the rule's
	/// <c>invalid_enum_value</c> code and message.
	/// </summary>
	/// <param name="writer">The writer positioned inside the generated <c>Validate</c> method.</param>
	/// <param name="property">The enum property the rule validates.</param>
	static void GenerateEnumValidation(CodeWriter writer, ZodPropertyDescriptor property)
	{
		var propertyName = property.Name;
		var ruleVariable = CodeGenHelpers.GetLocalIdentifier(propertyName, "EnumRule");
		var valueVariable = CodeGenHelpers.GetLocalIdentifier(propertyName, "EnumRuleValue");
		var pathFieldName = CodeGenHelpers.GetPathFieldName(propertyName);
		var ruleType = GetEnumRuleType(property);
		var zodRuleInterface = TypeLibrary.ZodSharp.Core.IZodRule.AsTypeReference().RenderFullName;

		// The rule is closed with the underlying enum type, so a nullable property is unwrapped. The
		// validation always runs inside the non-null guard, so the unwrap cannot throw.
		var valueExpression = property.IsNullableValueType ? $"value.{propertyName}!.Value" : $"value.{propertyName}";
		writer.Assignment("var", valueVariable, valueExpression);
		writer.Assignment("var", ruleVariable, $"new {ruleType}({GetDisallowedEnumValuesExpression(property)})");

		// The rule implements IZodRule, so it owns its error identity; the rule's constant is only the
		// fallback when the interface reports none. The cast is required because the interface may be
		// implemented explicitly.
		var codeExpression = $"(({zodRuleInterface}){ruleVariable}).Code ?? {ruleType}.ErrorCode";
		var originExpression = $"(({zodRuleInterface}){ruleVariable}).Origin";
		var messageExpression = GetEnumValidationMessageExpression(property, ruleVariable, valueVariable);

		writer.IfBlock(
			$"!{ruleVariable}.IsValid({valueVariable})",
			ifBody =>
			{
				ifBody.IfBlock(
					"errors is null",
					errorsBody =>
						errorsBody.Assignment(
							"errors",
							"new global::System.Collections.Generic.List<global::ZodSharp.Core.ValidationError>()"
						)
				);
				ifBody.MethodCallOn(
					"errors",
					"Add",
					$"{TypeLibrary.ZodSharp.Core.ValidationError}.Create({codeExpression}, {messageExpression}, {pathFieldName}, origin: {originExpression})"
				);
			}
		);

		writer.NewLine();
	}

	static string GetEnumRuleType(ZodPropertyDescriptor property) =>
		$"{TypeLibrary.ZodSharp.Rules.EnumRule.MakeGeneric(property.PropertyType)}";

	/// <summary>
	/// Builds the disallowed-member argument: the enum members marked <c>[ZodIgnore]</c> plus the values the
	/// property's <c>[DeniedValues]</c> attribute lists. Empty when every defined member is allowed.
	/// </summary>
	/// <param name="property">The enum property the rule validates.</param>
	/// <returns>The array-creation expression, or <see cref="string.Empty"/> for none.</returns>
	static string GetDisallowedEnumValuesExpression(ZodPropertyDescriptor property)
	{
		var deniedValues = property.ValidationAttributes.DeniedValues;
		if (property.IgnoredEnumMembers.Count == 0 && !(deniedValues.ShouldProcess && deniedValues.Value.Exists))
			return string.Empty;

		List<string> expressions = [.. property.IgnoredEnumMembers];
		var values = deniedValues.Value.Values;
		for (var i = 0; i < values.Count; i++)
			if (TryBuildTypedConstantExpression(property, values[i], out var expression))
				expressions.Add(expression);

		return $"new {property.PropertyType}[] {{{string.Join(", ", expressions)}}}";
	}

	/// <summary>
	/// Builds the message expression: a <c>[DeniedValues]</c> attribute that configures its own message is
	/// honoured, and otherwise the rule formats its own.
	/// </summary>
	/// <param name="property">The enum property the rule validates.</param>
	/// <param name="ruleVariable">The local the rule instance is assigned to.</param>
	/// <param name="valueVariable">The local holding the property value.</param>
	/// <returns>The message expression.</returns>
	static string GetEnumValidationMessageExpression(
		ZodPropertyDescriptor property,
		string ruleVariable,
		string valueVariable
	)
	{
		var deniedValues = property.ValidationAttributes.DeniedValues;
		var validationAttribute = deniedValues.Value.ValidationAttribute;

		if (deniedValues.ShouldProcess && deniedValues.Value.Exists && HasConfiguredMessage(validationAttribute))
		{
			return BuildErrorMessageExpression(
				validationAttribute,
				"Field '{0}' is invalid.",
				property.DisplayName.StringLiteral()
			);
		}

		// The rule formats its own message, so the property name is not needed.
		return $"{ruleVariable}.GetErrorMessage({valueVariable})";
	}

	static bool HasConfiguredMessage(ValidationAttributeData validationAttribute) =>
		!string.IsNullOrEmpty(validationAttribute.ErrorMessage)
		|| (
			!string.IsNullOrEmpty(validationAttribute.ErrorMessageResourceName)
			&& validationAttribute.ErrorMessageResourceType is not null
		);
}
