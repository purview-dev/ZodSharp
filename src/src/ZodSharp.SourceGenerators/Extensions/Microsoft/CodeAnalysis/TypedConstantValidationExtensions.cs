namespace Microsoft.CodeAnalysis;

static class TypedConstantValidationExtensions
{
	public static TypedConstantResult<T> Validate<T>(
		this TypedConstant constant,
		Func<T, bool> predicate,
		string failureMessage
	)
	{
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		var extracted = constant.GetValue<T>();
		if (!extracted.Success)
			return extracted;

		try
		{
			return predicate(extracted.Value!)
				? extracted
				: TypedConstantResult<T>.Fail(TypedConstantError.PredicateFailed, failureMessage);
		}
		catch (Exception exception)
		{
			return TypedConstantResult<T>.Fail(
				TypedConstantError.PredicateThrew,
				$"Validation failed unexpectedly: {exception.Message}"
			);
		}
	}
}
