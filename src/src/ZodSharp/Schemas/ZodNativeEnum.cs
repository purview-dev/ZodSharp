using System.Diagnostics.CodeAnalysis;
using ZodSharp.Core;

namespace ZodSharp.Schemas;

/// <summary>
/// Schema for native C# enum validation. Validates that a value is a defined
/// member of <typeparamref name="TEnum"/>. Equivalent to Zod's
/// <c>z.nativeEnum(Enum)</c>.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ZodNativeEnum{TEnum}"/> class.
/// </remarks>
[SuppressMessage("Naming", "CA1711", Justification = "Name mirrors Zod's z.nativeEnum API.")]
public class ZodNativeEnum<TEnum>() : ZodType<TEnum>
	where TEnum : struct, Enum
{
	static readonly string[] EmptyPath = [];

	// Resolving the defined members once per closed generic type avoids the per-validation
	// Enum.IsDefined reflection path.
	static readonly HashSet<TEnum> DefinedValues = [.. System.Enum.GetValues<TEnum>()];

	/// <summary>
	/// Validates that the value is a defined enum member.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <returns>A validation result.</returns>
	protected override ValidationResult<TEnum> ParseInternal(TEnum value) =>
		DefinedValues.Contains(value)
			? ValidationResult<TEnum>.Success(value)
			: ValidationResult<TEnum>.Failure(
				new ValidationError(
					"invalid_enum_value",
					$"'{value}' is not a defined member of {typeof(TEnum).Name}",
					EmptyPath
				)
			);

	/// <summary>
	/// Adds an <see cref="Rules.EnumRule{TEnum}"/> that validates the value is a defined member of
	/// <typeparamref name="TEnum"/>, rejecting any member the rule excludes.
	/// </summary>
	/// <param name="message">Optional error message.</param>
	/// <param name="code">Optional error code override.</param>
	/// <returns>This schema for method chaining.</returns>
	/// <remarks>
	/// The schema already rejects undefined members; this method mirrors the <c>[Enum]</c> attribute the
	/// source generator emits for enum properties and lets the error message and code be customised.
	/// </remarks>
	public ZodNativeEnum<TEnum> Enum(string? message = null, string? code = null)
	{
		AddRule(new Rules.EnumRule<TEnum>(message, code));
		return this;
	}
}
