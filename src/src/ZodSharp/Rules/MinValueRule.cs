namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for minimum numeric value.
/// Uses struct to avoid allocations.
/// </summary>
/// <typeparam name="T">The numeric type</typeparam>
public readonly record struct MinValueRule<T> : Core.IValidationRule<T>
	where T : IComparable<T>
{
	readonly T _minValue;

	/// <summary>
	/// Initializes a new instance of the MinValueRule struct.
	/// </summary>
	/// <param name="minValue">The minimum value</param>
	public MinValueRule(T minValue)
	{
		_minValue = minValue;
	}

	/// <summary>
	/// Validates that the value is greater than or equal to the minimum value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in T value) => value.CompareTo(_minValue) >= 0;

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in T value) => $"Value must be at least {_minValue}, but got {value}";
}
