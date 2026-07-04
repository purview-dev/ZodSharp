using System.Linq.Expressions;
using System.Reflection;
using ZodSharp.Core;
using ZodSharp.Schemas;

namespace ZodSharp.Expressions;

/// <summary>
/// Provides compiled validators using Expression Trees for maximum performance.
/// Inlines validation rules to avoid virtual calls and allocations.
/// </summary>
public static class CompiledValidator
{
	static readonly MethodInfo SuccessMethod = typeof(ValidationResult<>)
		.MakeGenericType(typeof(object))
		.GetMethod("Success", BindingFlags.Public | BindingFlags.Static)!;

	static readonly MethodInfo FailureMethod = typeof(ValidationResult<>)
		.MakeGenericType(typeof(object))
		.GetMethod("Failure", new[] { typeof(IEnumerable<ValidationError>) })!;

	/// <summary>
	/// Compiles a validator function from a schema using Expression Trees.
	/// This creates a highly optimized delegate that can be cached and reused.
	/// </summary>
	public static Func<T, ValidationResult<T>> Compile<T>(IZodSchema<T, T> schema)
	{
		if (schema is ZodType<T> zodType)
		{
			return CompileWithInlining<T>(zodType);
		}

		return CompileStandard<T>(schema);
	}

	/// <summary>
	/// Compiles with inlining of validation rules for maximum performance.
	/// </summary>
	static Func<T, ValidationResult<T>> CompileWithInlining<T>(ZodType<T> zodType)
	{
		var inputParam = Expression.Parameter(typeof(T), "input");

		var parseInternalMethod = zodType
			.GetType()
			.GetMethod(
				"ParseInternal",
				BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				new[] { typeof(T) },
				null
			);

		if (parseInternalMethod == null)
		{
			return CompileStandard<T>(zodType);
		}

		var parseResult = Expression.Call(Expression.Constant(zodType), parseInternalMethod, inputParam);

		var isSuccessProperty = typeof(ValidationResult<T>).GetProperty("IsSuccess")!;
		var isSuccess = Expression.Property(parseResult, isSuccessProperty);

		var resultVar = Expression.Variable(typeof(ValidationResult<T>), "result");
		var assignResult = Expression.Assign(resultVar, parseResult);

		var returnLabel = Expression.Label(typeof(ValidationResult<T>));
		var returnResult = Expression.Return(returnLabel, resultVar);
		var returnTarget = Expression.Label(returnLabel, Expression.Default(typeof(ValidationResult<T>)));

		var block = Expression.Block(
			new[] { resultVar },
			assignResult,
			Expression.IfThen(
				Expression.Not(Expression.Property(resultVar, isSuccessProperty)),
				Expression.Return(returnLabel, resultVar)
			),
			returnTarget
		);

		var lambda = Expression.Lambda<Func<T, ValidationResult<T>>>(block, inputParam);

		return lambda.Compile();
	}

	/// <summary>
	/// Standard compilation that calls Validate method.
	/// </summary>
	static Func<T, ValidationResult<T>> CompileStandard<T>(IZodSchema<T, T> schema)
	{
		var inputParam = Expression.Parameter(typeof(T), "input");
		var validateMethod = typeof(IZodSchema<T, T>).GetMethod(nameof(IZodSchema<T, T>.Validate))!;
		var validateCall = Expression.Call(Expression.Constant(schema), validateMethod, inputParam);

		var lambda = Expression.Lambda<Func<T, ValidationResult<T>>>(validateCall, inputParam);

		return lambda.Compile();
	}

	/// <summary>
	/// Compiles a validator that throws on failure.
	/// </summary>
	public static Func<T, T> CompileParser<T>(IZodSchema<T, T> schema)
	{
		var compiledValidator = Compile(schema);

		return input =>
		{
			var result = compiledValidator(input);
			if (!result.IsSuccess)
			{
				throw new ZodException(result.Errors);
			}
			return result.Value!;
		};
	}
}
