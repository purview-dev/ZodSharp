namespace Microsoft.CodeAnalysis;

static class TypedConstantExtensions
{
	public static TypedConstantResult<T> GetValue<T>(this TypedConstant constant)
	{
		if (constant.Kind == TypedConstantKind.Error)
		{
			return TypedConstantResult<T>.Fail(
				TypedConstantError.ErrorConstant,
				"The constant could not be resolved by Roslyn."
			);
		}

		if (constant.Kind == TypedConstantKind.Array)
		{
			return TypedConstantResult<T>.Fail(
				TypedConstantError.ArrayNotSupported,
				$"An array constant cannot be extracted as '{typeof(T)}'."
			);
		}

		if (constant.IsNull)
		{
			return CanBeNull<T>()
				? TypedConstantResult<T>.Ok(default)
				: TypedConstantResult<T>.Fail(
					TypedConstantError.NullNotAllowed,
					$"Null cannot be assigned to '{typeof(T)}'."
				);
		}

		if (constant.Value is T value)
		{
			return TypedConstantResult<T>.Ok(value);
		}

		var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

		if (TryConvertNumeric(constant.Value, targetType, out var converted) && converted is T convertedValue)
		{
			return TypedConstantResult<T>.Ok(convertedValue);
		}

		if (targetType.IsEnum && TryConvertEnum(constant.Value, targetType, out converted) && converted is T enumValue)
		{
			return TypedConstantResult<T>.Ok(enumValue);
		}

		// Failed to match or convert the constant value to the requested type
		return TypedConstantResult<T>.Fail(
			TypedConstantError.TypeMismatch,
			$"The constant value is '{constant.Value?.GetType()}', " + $"but '{typeof(T)}' was requested."
		);
	}

	static bool CanBeNull<T>() => default(T) is null;

	static bool TryConvertNumeric(object? value, Type targetType, out object? result)
	{
		result = null;

		if (value is null || !IsNumericType(value.GetType()) || !IsNumericType(targetType))
		{
			return false;
		}

		try
		{
			result = Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);

			return true;
		}
		catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
		{
			return false;
		}
	}

	static bool TryConvertEnum(object? value, Type enumType, out object? result)
	{
		result = null;

		if (value is null || !enumType.IsEnum)
		{
			return false;
		}

		try
		{
			var underlyingType = Enum.GetUnderlyingType(enumType);

			if (!TryConvertNumeric(value, underlyingType, out var underlyingValue))
			{
				return false;
			}

			result = Enum.ToObject(enumType, underlyingValue!);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	static bool IsNumericType(Type type)
	{
		type = Nullable.GetUnderlyingType(type) ?? type;

		return Type.GetTypeCode(type) switch
		{
			TypeCode.SByte
			or TypeCode.Byte
			or TypeCode.Int16
			or TypeCode.UInt16
			or TypeCode.Int32
			or TypeCode.UInt32
			or TypeCode.Int64
			or TypeCode.UInt64
			or TypeCode.Single
			or TypeCode.Double
			or TypeCode.Decimal => true,

			_ => false,
		};
	}
}
