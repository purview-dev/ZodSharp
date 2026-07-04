using BenchmarkDotNet.Attributes;
using ZodSharp;
using ZodSharp.Core;

namespace ZodSharp.Performance;

/// <summary>
/// Performance tests for transform operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class TransformPerformanceTests
{
	readonly IZodSchema<string, string> _toLowerSchema;
	readonly IZodSchema<string, string> _toUpperSchema;
	readonly IZodSchema<string, string> _trimSchema;
	readonly IZodSchema<string, string> _chainedTransformSchema;

	public TransformPerformanceTests()
	{
		_toLowerSchema = Z.String().ToLower();
		_toUpperSchema = Z.String().ToUpper();
		_trimSchema = Z.String().Trim();
		_chainedTransformSchema = Z.String().Trim().ToLower();
	}

	[Benchmark]
	public ValidationResult<string> TransformToLower()
	{
		return _toLowerSchema.Validate("HELLO WORLD");
	}

	[Benchmark]
	public ValidationResult<string> TransformToUpper()
	{
		return _toUpperSchema.Validate("hello world");
	}

	[Benchmark]
	public ValidationResult<string> TransformTrim()
	{
		return _trimSchema.Validate("  hello world  ");
	}

	[Benchmark]
	public ValidationResult<string> TransformChained()
	{
		return _chainedTransformSchema.Validate("  HELLO WORLD  ");
	}

	[Benchmark]
	public ValidationResult<string> TransformWithValidation()
	{
		var schema = Z.String().Min(5).Max(100).Trim().ToLower().Email();
		return schema.Validate("  USER@EXAMPLE.COM  ");
	}
}
