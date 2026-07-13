# ZodSharp Project Agents

This document outlines instructions for agents working on the ZodSharp project.

## Prerequisites & Environment Setup

Ensure you have the following installed and configured:

- **.NET SDK:** Version X.Y.Z (check `global.json` if present, or inquire if unsure)
- **CSharpier:** Used via `dotnet csharpier` - this is made available by calling `dotnet tool restore`.
- **(Optional) IDE:** Visual Studio / VS Code with C# extensions recommended for development and debugging.

## Building the Project

The primary build command uses `dotnet`.

- **Build Solution:** `dotnet build ./src/ZodSharp.slnx`
- **Restore Dependencies:** `dotnet restore ./src/ZodSharp.slnx`

## Finding Files

- **Solution File:** `src/ZodSharp.slnx`
- **Source Code:** Located primarily within the `src/` directory. Key modules include:
  - `src/src/ZodSharp/`: Contains core logic, schemas, rules, etc.
  - `src/src/AspNetCore/`: Contains ASP.NET ZodSharp integrations.
  - `src/src/Examples.CLI/`: Example CLI applications.
  - `src/src/SourceGenerators/`: Source generators.
  - `src/src/SystemTextJson/`: System.Text.Json integration.
  - `src/src/NewtonsoftJson/`: Newtonsoft.Json integration.

- **Tests:** Unit and Integration tests are in `src/tests/`. Performance tests are in `src/tests/ZodSharp.PerformanceTests/`.
- **Artifacts (e.g., NuGet packages):** Built to `./artifacts/`

## Running Tests

The project uses `dotnet test`.

- **All Test Projects:** `dotnet tests ./src/ZodSharp.slnx --treenode-filter="/*/*/*/*/"`

**Important:** When running tests, adhere to TUnit best practices for robust, maintainable, and easily debuggable tests. This includes:

- Asynchronous assertions must be `await`ed.
- Follow the Arrange-Act-Assert pattern.
- Ensure tests are independent and idempotent.
- Utilize data-driven tests where appropriate.
- Refer to the `csharp-tunit` skill documentation for advanced patterns.
- Never use FluentAssertions, Xunit, or NUnit.

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
