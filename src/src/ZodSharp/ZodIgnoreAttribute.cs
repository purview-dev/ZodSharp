namespace ZodSharp;

/// <summary>
/// Marks an enum member as not a valid value, so the source generator's automatic enum validation rejects it.
/// </summary>
/// <remarks>
/// <para>
/// Apply to an enum member to exclude it from the allowed set everywhere that enum is validated by a
/// generated schema. A common case is an <c>Unspecified</c>-style member that is defined for wire
/// compatibility but is never a valid field value:
/// </para>
/// <code>
/// enum ExampleEnum
/// {
///     [ZodIgnore]
///     Unspecified,
///
///     AValidValue,
///
///     AnotherValidValue
/// }
/// </code>
/// <para>
/// The exclusion applies to the automatic enum validation the <c>[ZodSchema]</c> generator emits
/// (<c>[ZodSchema(ValidateEnumValues = false)]</c> opts out) and composes with a property's
/// <c>[DeniedValues]</c> attribute, whose values are excluded for that property only.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ZodIgnoreAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ZodIgnoreAttribute"/> class.
	/// </summary>
	public ZodIgnoreAttribute() { }
}
