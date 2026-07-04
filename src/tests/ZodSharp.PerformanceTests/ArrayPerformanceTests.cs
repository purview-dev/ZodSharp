using BenchmarkDotNet.Attributes;
using ZodSharp;
using ZodSharp.Core;

namespace ZodSharp.Performance;

/// <summary>
/// Performance tests for array validation scenarios with varying sizes.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class ArrayPerformanceTests
{
	readonly IZodSchema<string[], string[]> _smallArraySchema;
	readonly IZodSchema<string[], string[]> _mediumArraySchema;
	readonly IZodSchema<string[], string[]> _largeArraySchema;
	readonly IZodSchema<double[], double[]> _numberArraySchema;

	readonly string[] _smallArray;
	readonly string[] _mediumArray;
	readonly string[] _largeArray;
	readonly double[] _numberArray;

	public ArrayPerformanceTests()
	{
		_smallArraySchema = Z.Array(Z.String().Min(1).Max(10)).Min(1).Max(10);
		_mediumArraySchema = Z.Array(Z.String().Email()).Min(1).Max(100);
		_largeArraySchema = Z.Array(Z.String().Min(1).Max(50)).Min(1).Max(1000);
		_numberArraySchema = Z.Array(Z.Number().Min(0).Max(100).Int()).Min(1).Max(1000);

		_smallArray = Enumerable.Range(1, 10).Select(i => $"item{i}").ToArray();
		_mediumArray = Enumerable.Range(1, 100).Select(i => $"user{i}@example.com").ToArray();
		_largeArray = Enumerable.Range(1, 1000).Select(i => $"item{i}").ToArray();
		_numberArray = Enumerable.Range(1, 1000).Select(i => (double)i).ToArray();
	}

	[Benchmark]
	public ValidationResult<string[]> ValidateSmallArray()
	{
		return _smallArraySchema.Validate(_smallArray);
	}

	[Benchmark]
	public ValidationResult<string[]> ValidateMediumArray()
	{
		return _mediumArraySchema.Validate(_mediumArray);
	}

	[Benchmark]
	public ValidationResult<string[]> ValidateLargeArray()
	{
		return _largeArraySchema.Validate(_largeArray);
	}

	[Benchmark]
	public ValidationResult<double[]> ValidateNumberArray()
	{
		return _numberArraySchema.Validate(_numberArray);
	}

	[Benchmark]
	public ValidationResult<string[]> ValidateLargeArrayWithComplexSchema()
	{
		var schema = Z.Array(
			Z.String().Min(5).Max(100).Email().Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
		);
		return schema.Validate(_mediumArray);
	}

	[Benchmark]
	public ValidationResult<string[]> ValidateLargeArrayInvalid()
	{
		var invalid = _largeArray.Concat(new[] { "" }).ToArray();
		return _largeArraySchema.Validate(invalid);
	}
}
