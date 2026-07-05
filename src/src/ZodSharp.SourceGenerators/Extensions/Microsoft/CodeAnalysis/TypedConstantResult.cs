namespace Microsoft.CodeAnalysis;

enum TypedConstantError
{
	None,
	ErrorConstant,
	ArrayNotSupported,
	NullNotAllowed,
	MissingType,
	TypeMismatch,
	UnsupportedType,
	PredicateFailed,
	PredicateThrew,
}

readonly record struct TypedConstantResult<T>(bool Success, T? Value, TypedConstantError Error, string? Message)
{
	public static TypedConstantResult<T> Ok(T? value) => new(true, value, TypedConstantError.None, null);

	public static TypedConstantResult<T> Fail(TypedConstantError error, string message) =>
		new(false, default, error, message);

	public override string ToString() => Success ? $"{Value}" : $"Error: {Error}, Message: {Message}";
}
