# ZodSharp Project Agents

This document outlines instructions for agents working on the ZodSharp project.

## Prerequisites & Environment Setup

Ensure you have the following installed and configured:

- **.NET SDK:** Version X.Y.Z (check `global.json` if present, or inquire if unsure)
- **Just:** `cargo install just` or download from releases.
- **CSharpier:** Ensure the CSharpier executable is in your PATH or discoverable by `just`.
- **(Optional) IDE:** Visual Studio / VS Code with C# extensions recommended for development and debugging.

## Building the Project

The primary build command uses `just`. These commands leverage defaults defined in the `Justfile` but allow for explicit overrides.

- **Build Solution:** `just build`
  - *Defaults:* If `solutionOrProject` and `configuration` are omitted, `just build` uses `src/ZodSharp.slnx` and the `Release` configuration respectively.
  - *Example Override:* `just build src/ZodSharp.slnx configuration=Debug`
- **Restore Dependencies:** `just restore`
  - *Details:* Ensures all project dependencies are correctly restored, handling .NET SDK and NuGet package restores. The `solutionOrProject` parameter defaults to `src/ZodSharp.slnx`.

## Finding Files

- **Solution File:** `src/ZodSharp.slnx`
- **Source Code:** Located primarily within the `src/` directory. Key modules include:
  - `src/src/ZodSharp/`: Contains core logic, schemas, rules, etc.
  - `src/src/ZodSharp.Examples.CLI/`: Example CLI applications.
  - `src/src/ZodSharp.SourceGenerators/`: Source generators.
  - `src/src/ZodSharp.SystemTextJson/`: System.Text.Json integration.
  - `src/src/ZodSharp.NewtonsoftJson/`: Newtonsoft.Json integration.

- **Tests:** Unit tests are in `src/tests/`. Performance tests are in `src/tests/ZodSharp.PerformanceTests/`.
- **Artifacts (e.g., NuGet packages):** Built to `./artifacts/`

## Running Tests

The project uses `dotnet test`, orchestrated by `just`. These commands leverage defaults defined in the `Justfile` but allow for explicit overrides.

- **All Unit Tests:** `just tests solutionOrProject=src/ZodSharp.slnx configuration=Release filter="/*/*/*/*/" *args`
  - *Defaults:* If `solutionOrProject`, `configuration`, and `filter` are omitted, `just tests` uses `src/ZodSharp.slnx`, `Release` configuration, and `/*/*/*/*/` filter respectively. `*args` can be used for additional `dotnet test` arguments.
  - *Example Override:* `just tests src/tests --filter "FullyQualifiedName~Your.Namespace.Tests.YourTestClass.YourTestMethod"`
- **Performance Tests:** `just perf-tests configuration=Release *args`
  - *Defaults:* Uses the `Release` configuration and targets the `src/tests/ZodSharp.PerformanceTests/ZodSharp.PerformanceTests.csproj` project. `*args` can be used for additional `dotnet run` arguments.
  - *Example Usage:* `just perf-tests configuration=Debug`

**Important:** When running tests, adhere to TUnit best practices for robust, maintainable, and easily debuggable tests. This includes:

- Asynchronous assertions must be `await`ed.
- Follow the Arrange-Act-Assert pattern.
- Ensure tests are independent and idempotent.
- Utilize data-driven tests where appropriate.
- Refer to the `csharp-tunit` skill documentation for advanced patterns.

## Styling & Linting

Code style and formatting are managed by CSharpier. All code contributions must pass these checks.

- **Check Formatting:** `just lint-check`
- **Fix Formatting:** `just lint-fix`

## Agent Rules & Guidelines

1. **Code Changes:**

- Adhere to project coding standards and conventions.
- Ensure all new code has corresponding unit tests adhering to TUnit best practices.
- Run `just lint-check` before completing work.
- Run `just tests` to confirm all tests pass before completing work.

1. **Commit Messages:** Use a clear and concise commit message format (e.g., Conventional Commits).
1. **Pull Requests:** All significant changes should be submitted via Pull Requests and undergo review. Ensure `just lint-check` and `just tests` pass before submitting.
1. **Building Artifacts:** `just pack`
   - *Defaults:* Uses `src/ZodSharp.slnx`, `Release` configuration, and outputs to the `./artifacts/` folder.
