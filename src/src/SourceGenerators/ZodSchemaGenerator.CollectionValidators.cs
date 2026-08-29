using System.Globalization;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void GenerateCollectionValidations(CodeWriter writer, ZodPropertyDescriptor property)
	{
		var lengthAttr = property.ValidationAttributes.Length;
		var minLengthAttr = property.ValidationAttributes.MinLength;
		var maxLengthAttr = property.ValidationAttributes.MaxLength;

		if (
			(!lengthAttr.ShouldProcess || !lengthAttr.Value.Exists)
			&& (!minLengthAttr.ShouldProcess || !minLengthAttr.Value.Exists)
			&& (!maxLengthAttr.ShouldProcess || !maxLengthAttr.Value.Exists)
		)
		{
			GenerateCollectionElementValidation(writer, property);
			return;
		}

		var lengthAccessor = property.LengthAccessor;
		if (!lengthAccessor.IsSupported)
		{
			GenerateCollectionElementValidation(writer, property);
			return;
		}

		var propertyName = property.Name;
		var displayName = property.DisplayName;
		var propertyValueName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Value");
		var propertyLengthName = CodeGenHelpers.GetLocalIdentifier(propertyName, "Length");
		var origin = lengthAccessor.Origin;

		writer.WriteLine($"var {propertyValueName} = value.{propertyName};");
		using (writer.OpenBlockScope($"if ({propertyValueName} is not null)"))
		{
			writer.WriteLine($"var propertyValue = {propertyValueName};");
			writer.WriteLine($"var {propertyLengthName} = {lengthAccessor.LengthExpression};");

			if (lengthAttr.ShouldProcess && lengthAttr.Value.Exists)
			{
				var length = lengthAttr.Value;
				if (length.MinimumLength >= 0 && length.MaximumLength >= length.MinimumLength)
				{
					var tooSmallMessage = BuildMessageExpression(
						length.ValidationAttribute,
						$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain at least ")} + FormatCount({length.MinimumLength}, {CodeGenHelpers.Quote("element")}, {CodeGenHelpers.Quote("elements")}) + {CodeGenHelpers.Quote(".")}",
						CodeGenHelpers.Quote(displayName),
						length.MaximumLength.ToString(CultureInfo.InvariantCulture),
						length.MinimumLength.ToString(CultureInfo.InvariantCulture)
					);
					var tooBigMessage = BuildMessageExpression(
						length.ValidationAttribute,
						$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({length.MaximumLength}, {CodeGenHelpers.Quote("element")}, {CodeGenHelpers.Quote("elements")}) + {CodeGenHelpers.Quote(".")}",
						CodeGenHelpers.Quote(displayName),
						length.MaximumLength.ToString(CultureInfo.InvariantCulture),
						length.MinimumLength.ToString(CultureInfo.InvariantCulture)
					);

					using (writer.OpenBlockScope($"if ({propertyLengthName} < {length.MinimumLength})"))
					{
						WriteValidationError(
							writer,
							"too_small",
							tooSmallMessage,
							CodeGenHelpers.GetPathFieldName(propertyName),
							origin,
							minimum: length.MinimumLength
						);
					}

					using (writer.OpenBlockScope($"else if ({propertyLengthName} > {length.MaximumLength})"))
					{
						WriteValidationError(
							writer,
							"too_big",
							tooBigMessage,
							CodeGenHelpers.GetPathFieldName(propertyName),
							origin,
							maximum: length.MaximumLength
						);
					}
				}
			}

			if (minLengthAttr.ShouldProcess && minLengthAttr.Value.Exists && minLengthAttr.Value.Length > 0)
			{
				var minLength = minLengthAttr.Value.Length;
				var messageExpression = BuildMessageExpression(
					minLengthAttr.Value.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain at least ")} + FormatCount({minLength}, {CodeGenHelpers.Quote("element")}, {CodeGenHelpers.Quote("elements")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					minLength.ToString(CultureInfo.InvariantCulture)
				);

				using (writer.OpenBlockScope($"if ({propertyLengthName} < {minLength})"))
				{
					WriteValidationError(
						writer,
						"too_small",
						messageExpression,
						CodeGenHelpers.GetPathFieldName(propertyName),
						origin,
						minimum: minLength
					);
				}
			}

			if (maxLengthAttr.ShouldProcess && maxLengthAttr.Value.Exists && maxLengthAttr.Value.Length >= 0)
			{
				var maxLength = maxLengthAttr.Value.Length;
				var messageExpression = BuildMessageExpression(
					maxLengthAttr.Value.ValidationAttribute,
					$"{CodeGenHelpers.Quote($"Field '{displayName}' must contain no more than ")} + FormatCount({maxLength}, {CodeGenHelpers.Quote("element")}, {CodeGenHelpers.Quote("elements")}) + {CodeGenHelpers.Quote(".")}",
					CodeGenHelpers.Quote(displayName),
					maxLength.ToString(CultureInfo.InvariantCulture)
				);

				using (writer.OpenBlockScope($"if ({propertyLengthName} > {maxLength})"))
				{
					WriteValidationError(
						writer,
						"too_big",
						messageExpression,
						CodeGenHelpers.GetPathFieldName(propertyName),
						origin,
						maximum: maxLength
					);
				}
			}
		}

		writer.NewLine();
		GenerateCollectionElementValidation(writer, property);
	}
}
