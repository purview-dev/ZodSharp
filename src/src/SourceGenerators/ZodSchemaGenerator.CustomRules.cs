using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	/// <summary>
	/// Emits validation for rules bound to the property through DataAnnotations-style attributes that
	/// carry <c>[ZodRule(typeof(...))]</c>. The emitted call reuses the rule's own
	/// <c>IsValid</c>/<c>GetErrorMessage</c> contract, so a custom rule behaves exactly like a built-in one.
	/// </summary>
	static void GenerateCustomRuleValidations(CodeWriter writer, ZodPropertyDescriptor property)
	{
		if (property.CustomRules.Count == 0)
			return;

		// The resolver closes a generic rule over the property's underlying type, so a nullable value type is
		// unwrapped to match. The validation runs inside the non-null guard, so the unwrap cannot throw.
		var valueExpression = property.IsNullableValueType ? $"value.{property.Name}!.Value" : $"value.{property.Name}";

		GenerateRuleValidations(
			writer,
			property.CustomRules,
			property.Name,
			valueExpression,
			CodeGenHelpers.GetPathFieldName(property.Name),
			property.DisplayName,
			declareValueLocal: true
		);
	}

	/// <summary>
	/// Emits type-level rules: <c>[ZodRule]</c>-mapped attributes applied to the target type itself. They
	/// validate the whole value (the value object as a unit) and report an empty path.
	/// </summary>
	static void GenerateTypeRuleValidations(CodeWriter writer, ZodSchemaDescriptor schema)
	{
		if (schema.TypeRules.Count == 0)
			return;

		GenerateRuleValidations(
			writer,
			schema.TypeRules,
			schema.TargetType.Name,
			"value",
			"EmptyPath",
			schema.TargetType.Name,
			declareValueLocal: false
		);
	}

	static void GenerateRuleValidations(
		CodeWriter writer,
		EquatableArray<CustomRuleDescriptor> rules,
		string localPrefix,
		string valueExpression,
		string pathExpression,
		string displayName,
		bool declareValueLocal
	)
	{
		for (var i = 0; i < rules.Count; i++)
		{
			var rule = rules[i];
			var ruleVariable = CodeGenHelpers.GetLocalIdentifier(localPrefix, $"CustomRule{i}");
			var valueVariable = declareValueLocal
				? CodeGenHelpers.GetLocalIdentifier(localPrefix, $"CustomRuleValue{i}")
				: valueExpression;
			var arguments = $"({string.Join(", ", rule.Arguments)})";

			if (declareValueLocal)
				writer.Assignment("var", valueVariable, valueExpression);

			// A scalar rule adapter wraps a rule written against the scalar's underlying value. The wrapped
			// rule is constructed first so it can supply the error identity (the adapter itself has none).
			var identityVariable = ruleVariable;
			if (rule.AdaptedFrom is { } adaptedRuleType)
			{
				identityVariable = CodeGenHelpers.GetLocalIdentifier(localPrefix, $"CustomRuleInner{i}");
				writer.Assignment(
					"var",
					identityVariable,
					$"new {adaptedRuleType.AsTypeReference().RenderFullName}{arguments}"
				);
				writer.Assignment(
					"var",
					ruleVariable,
					$"new {rule.RuleType.AsTypeReference().RenderFullName}({identityVariable})"
				);
			}
			else
			{
				writer.Assignment(
					"var",
					ruleVariable,
					$"new {rule.RuleType.AsTypeReference().RenderFullName}{arguments}"
				);
			}

			var codeFallback = rule.Code is { Length: > 0 } customCode ? customCode : "validation_failed";
			var zodRuleInterface = TypeLibrary.ZodSharp.Core.IZodRule.AsTypeReference().RenderFullName;

			// A rule that implements IZodRule owns its error identity; the attribute-mapped value is only a
			// fallback. The cast is required because the interface may be implemented explicitly.
			var codeExpression = rule.RuleOwnsIdentity
				? $"(({zodRuleInterface}){identityVariable}).Code ?? {codeFallback.Surround()}"
				: codeFallback.Surround();
			var originFallback = rule.Origin is { Length: > 0 } customOrigin ? customOrigin.Surround() : null;
			var originExpression = rule.RuleOwnsIdentity
				? originFallback is null
					? $"(({zodRuleInterface}){identityVariable}).Origin"
					: $"(({zodRuleInterface}){identityVariable}).Origin ?? {originFallback}"
				: originFallback ?? "null";

			var message = !string.IsNullOrEmpty(rule.Message.ErrorMessage)
				? BuildErrorMessageExpression(rule.Message, "Field '{0}' is invalid.", displayName.StringLiteral())
				: $"{ruleVariable}.GetErrorMessage({valueVariable})";

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
						$"{TypeLibrary.ZodSharp.Core.ValidationError}.Create({codeExpression}, {message}, {pathExpression}, origin: {originExpression})"
					);
				}
			);

			writer.NewLine();
		}
	}
}
