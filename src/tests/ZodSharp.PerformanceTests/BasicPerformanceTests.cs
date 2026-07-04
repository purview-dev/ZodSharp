using BenchmarkDotNet.Attributes;
using ZodSharp;
using ZodSharp.Core;

namespace ZodSharp.Performance;

/// <summary>
/// Basic performance tests for common validation scenarios.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class BasicPerformanceTests
{
	readonly IZodSchema<string, string> _stringSchema;
	readonly IZodSchema<double, double> _numberSchema;
	readonly IZodSchema<bool, bool> _booleanSchema;
	readonly IZodSchema<string[], string[]> _arraySchema;

	public BasicPerformanceTests()
	{
		_stringSchema = Z.String().Min(3).Max(50).Email();
		_numberSchema = Z.Number().Min(0).Max(100).Int();
		_booleanSchema = Z.Boolean();
		_arraySchema = Z.Array(Z.String()).Min(1).Max(10);
	}

	[Benchmark]
	public ValidationResult<string> ValidateString()
	{
		return _stringSchema.Validate("user@example.com");
	}

	[Benchmark]
	public ValidationResult<double> ValidateNumber()
	{
		return _numberSchema.Validate(42.0);
	}

	[Benchmark]
	public ValidationResult<bool> ValidateBoolean()
	{
		return _booleanSchema.Validate(true);
	}

	[Benchmark]
	public ValidationResult<string[]> ValidateStringArray()
	{
		return _arraySchema.Validate(new[] { "a", "b", "c", "d", "e" });
	}

	[Benchmark]
	public ValidationResult<string> ValidateStringWithMultipleRules()
	{
		var schema = Z.String().Min(5).Max(100).Email().Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
		return schema.Validate("test@example.com");
	}

	[Benchmark]
	public ValidationResult<double> ValidateNumberWithMultipleRules()
	{
		var schema = Z.Number().Min(0).Max(100).Int().MultipleOf(2);
		return schema.Validate(42.0);
	}
}
