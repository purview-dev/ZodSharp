namespace ZodSharp.Rules;

/// <summary>
/// Validation rule for maximum numeric value.
/// Uses struct to avoid allocations.
/// </summary>
/// <typeparam name="T">The numeric type</typeparam>
public readonly struct MaxValueRule<T> : Core.IValidationRule<T>
	where T : IComparable<T>
{
	readonly T _maxValue;

	/// <summary>
	/// Initializes a new instance of the MaxValueRule struct.
	/// </summary>
	/// <param name="maxValue">The maximum value</param>
	public MaxValueRule(T maxValue)
	{
		_maxValue = maxValue;
	}

	/// <summary>
	/// Validates that the value is less than or equal to the maximum value.
	/// </summary>
	/// <param name="value">The value to validate</param>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid(in T value)
	{
		return value.CompareTo(_maxValue) <= 0;
	}

	/// <summary>
	/// Gets the error message for a failed validation.
	/// </summary>
	/// <param name="value">The value that failed validation</param>
	/// <returns>The error message</returns>
	public string GetErrorMessage(in T value)
	{
		return $"Value must be at most {_maxValue}, but got {value}";
	}
}
