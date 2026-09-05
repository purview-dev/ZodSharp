# ZodSharp Performance Tests

This folder contains comprehensive performance tests for the ZodSharp library using BenchmarkDotNet.

## Structure

- **BasicPerformanceTests.cs**: Basic validation tests (strings, numbers, simple arrays)
- **ObjectPerformanceTests.cs**: Object validation tests (simple, medium, complex)
- **ArrayPerformanceTests.cs**: Tests with arrays of different sizes
- **HeavyPerformanceTests.cs**: Heavy tests (deeply nested objects, many fields, refinements)
- **MemoryPerformanceTests.cs**: Tests focused on memory allocations
- **TransformPerformanceTests.cs**: Transformation tests (ToLower, ToUpper, Trim)
- **UnionPerformanceTests.cs**: Union and discriminated union tests

## How to Run

### Build and Run

```bash
# Build the project
dotnet build ZodSharp\ZodSharp.sln

# Run all benchmarks
dotnet run --project ZodSharp\performance\performance.csproj -c Release
```

### Run Specific Benchmarks

```bash
# Run only basic tests
dotnet run --project ZodSharp\performance\performance.csproj -c Release --filter "*BasicPerformanceTests*"

# Run only memory tests
dotnet run --project ZodSharp\performance\performance.csproj -c Release --filter "*MemoryPerformanceTests*"
```

## Results

Benchmark results are saved in the `BenchmarkDotNet.Artifacts` folder after execution. You will find:

- **HTML Reports**: Detailed visualization of results
- **Markdown Reports**: Summary of results
- **Logs**: Detailed execution logs

## Test Cases

### Basic Cases
- Simple string validation
- Number validation
- Small array validation
- Validation with multiple rules

### Medium Cases
- Objects with 5-10 fields
- Arrays with 100 elements
- Validations with multiple chained rules

### Heavy Cases
- Deeply nested objects (4+ levels)
- Objects with 50+ fields
- Arrays with 1000+ elements
- Multiple chained refinements
- Large objects with nested arrays

### Memory Tests
- Allocations per validation
- Multiple iterations to identify leaks
- Comparison of allocations between different validation types

## Interpreting Results

### Important Metrics

1. **Mean**: Average execution time
2. **Error**: Error margin
3. **StdDev**: Standard deviation
4. **Gen 0/1/2**: Garbage collections per generation
5. **Allocated**: Memory allocated per operation

### Identifying Performance Issues

- **High time**: Check for unnecessary operations or inefficient loops
- **High allocations**: Look for unnecessary allocations, especially in hot paths
- **Gen 2 collections**: Indicates long-lived objects that can be optimized

## Optimization Tips

1. Run benchmarks in Release mode for more accurate results
2. Run multiple times to ensure consistency
3. Compare results before and after optimizations
4. Use MemoryDiagnoser to identify unnecessary allocations
5. Focus on optimizing the most common cases first

## Adding New Tests

To add new performance tests:

1. Create a new class inheriting from `BenchmarkDotNet.Attributes`
2. Add the `[MemoryDiagnoser]` attribute for memory analysis
3. Add the `[SimpleJob]` attribute to configure the benchmark
4. Create methods marked with `[Benchmark]`
5. Run and analyze the results

Exemplo:

```csharp
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class MyPerformanceTests
{
    [Benchmark]
    public ValidationResult<string> MyTest()
    {
        // Seu código de teste aqui
    }
}
```

