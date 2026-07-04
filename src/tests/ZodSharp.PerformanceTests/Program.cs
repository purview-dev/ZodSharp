using BenchmarkDotNet.Running;
using ZodSharp.Performance;

Console.WriteLine("=== ZodSharp Performance Tests ===\n");
Console.WriteLine("Running performance benchmarks...\n");

var summary = BenchmarkRunner.Run(typeof(Program).Assembly);

Console.WriteLine("\n=== Performance Test Summary ===");
Console.WriteLine("Check the 'BenchmarkDotNet.Artifacts' folder for detailed results.");
