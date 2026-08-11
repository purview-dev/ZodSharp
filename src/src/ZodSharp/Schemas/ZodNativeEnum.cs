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

	/// <summary>
	/// Validates that the value is a defined enum member.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <returns>A validation result.</returns>
	protected override ValidationResult<TEnum> ParseInternal(TEnum value) =>
#if NET5_0_OR_GREATER
		Enum.IsDefined(value)
#else
		Enum.IsDefined(typeof(TEnum), value)
#endif
			? ValidationResult<TEnum>.Success(value)
			: ValidationResult<TEnum>.Failure(
				new ValidationError(
					"invalid_enum_value",
					$"'{value}' is not a defined member of {typeof(TEnum).Name}",
					EmptyPath
				)
			);
}
