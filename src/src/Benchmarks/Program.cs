using BenchmarkDotNet.Running;

Console.WriteLine("=== ZodSharp Performance Tests ===\n");
Console.WriteLine("Running performance benchmarks...\n");

// Preserve the original run-all behaviour when no arguments are supplied (for `just perf-tests`),
// but honour BenchmarkDotNet arguments such as `--filter` when they are provided.
if (args.Length == 0)
	BenchmarkRunner.Run(typeof(Program).Assembly);
else
	BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

Console.WriteLine("\n=== Performance Test Summary ===");
Console.WriteLine("Check the 'BenchmarkDotNet.Artifacts' folder for detailed results.");
