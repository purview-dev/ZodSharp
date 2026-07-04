using BenchmarkDotNet.Attributes;
using ZodSharp;
using ZodSharp.Core;

namespace ZodSharp.Performance;

/// <summary>
/// Performance tests for union and discriminated union scenarios.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class UnionPerformanceTests
{
	readonly IZodSchema<object, object> _unionSchema;
	readonly IZodSchema<object, object> _discriminatedUnionSchema;

	readonly Dictionary<string, object?> _discriminatedObject1;
	readonly Dictionary<string, object?> _discriminatedObject2;

	public UnionPerformanceTests()
	{
		var stringSchema = Z.String().Email();
		var numberSchema = Z.Number().Min(0).Max(100);
		var boolSchema = Z.Boolean();

		_unionSchema = Z.Union(
			new SchemaWrapper<string>(stringSchema),
			new SchemaWrapper<double>(numberSchema),
			new SchemaWrapper<bool>(boolSchema)
		);

		var userSchema = Z.Object()
			.Field("type", Z.Literal("user"))
			.Field("name", Z.String().Min(1))
			.Field("email", Z.String().Email())
			.Build();

		var adminSchema = Z.Object()
			.Field("type", Z.Literal("admin"))
			.Field("name", Z.String().Min(1))
			.Field("permissions", Z.Array(Z.String()).Min(1))
			.Build();

		_discriminatedUnionSchema = Z.DiscriminatedUnion("type")
			.Option("user", new SchemaWrapper<Dictionary<string, object?>>(userSchema))
			.Option("admin", new SchemaWrapper<Dictionary<string, object?>>(adminSchema))
			.Build();

		_discriminatedObject1 = new Dictionary<string, object?>
		{
			{ "type", "user" },
			{ "name", "John" },
			{ "email", "john@example.com" },
		};

		_discriminatedObject2 = new Dictionary<string, object?>
		{
			{ "type", "admin" },
			{ "name", "Admin" },
			{ "permissions", new[] { "read", "write", "delete" } },
		};
	}

	class SchemaWrapper<T> : IZodSchema<object, object>
	{
		readonly IZodSchema<T, T> _inner;

		public SchemaWrapper(IZodSchema<T, T> inner)
		{
			_inner = inner;
		}

		public ValidationResult<object> Validate(object value)
		{
			if (value is T typedValue)
			{
				var result = _inner.Validate(typedValue);
				if (result.IsSuccess)
				{
					return ValidationResult<object>.Success(result.Value!);
				}
				return ValidationResult<object>.Failure(result.Errors);
			}
			return ValidationResult<object>.Failure(
				new ValidationError(
					"invalid_type",
					$"Expected {typeof(T).Name}, but got {value?.GetType().Name ?? "null"}",
					Array.Empty<string>()
				)
			);
		}

		public ValueTask<ValidationResult<object>> ValidateAsync(object value)
		{
			return new ValueTask<ValidationResult<object>>(Validate(value));
		}
	}

	[Benchmark]
	public ValidationResult<object> ValidateUnion_String()
	{
		return _unionSchema.Validate("user@example.com");
	}

	[Benchmark]
	public ValidationResult<object> ValidateUnion_Number()
	{
		return _unionSchema.Validate(42.0);
	}

	[Benchmark]
	public ValidationResult<object> ValidateUnion_Boolean()
	{
		return _unionSchema.Validate(true);
	}

	[Benchmark]
	public ValidationResult<object> ValidateDiscriminatedUnion_FirstOption()
	{
		return _discriminatedUnionSchema.Validate(_discriminatedObject1);
	}

	[Benchmark]
	public ValidationResult<object> ValidateDiscriminatedUnion_SecondOption()
	{
		return _discriminatedUnionSchema.Validate(_discriminatedObject2);
	}

	[Benchmark]
	public ValidationResult<object> ValidateUnion_Invalid()
	{
		return _unionSchema.Validate("invalid");
	}
}
