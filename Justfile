set quiet

root_folder := "src"
solution := root_folder / "ZodSharp.slnx"
perf_tests_project := root_folder / "tests" / "ZodSharp.PerformanceTests" / "ZodSharp.PerformanceTests.csproj"
build_configuration := "Release"
artifacts_folder := "./artifacts"
default_test_filter:= "/*/*/*/*/"

[private]
default:
    just --list

# Build and test with the specified configuration, defaulting to "Release"
build solutionOrProject=solution configuration=build_configuration:
    echo "Building {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }}"
    dotnet build {{ solutionOrProject }} -c {{ configuration }}

# Run the performance tests with the specified configuration, defaulting to "Release"
perf-tests configuration=build_configuration *args:
    echo "Running performance tests for {{ BLUE }}{{ perf_tests_project }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }}"
    dotnet run --project {{ perf_tests_project }} -c {{ configuration }} {{ args }}

# Run tests with the specified configuration, defaulting to "Release"
tests solutionOrProject=solution configuration=build_configuration filter=default_test_filter *args:
    echo "Running tests for {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }} and filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test {{ solutionOrProject }} -c {{ configuration }} --treenode-filter "{{ filter }}" {{ args }}

# Run tests with the specified configuration, defaulting to "Release"
restore solutionOrProject=solution:
    echo "Restoring dependencies for {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }}"
    dotnet restore {{ solutionOrProject }}

# Create NuGet package for the project
pack solutionOrProject=solution configuration=build_configuration publish_folder=artifacts_folder:
    echo "Packing {{ BLUE }}{{ solutionOrProject }}{{ NORMAL }} with configuration {{ YELLOW }}{{ configuration }}{{ NORMAL }} to {{ GREEN }}{{ publish_folder }}{{ NORMAL }}"
    dotnet pack {{ solutionOrProject }} -c {{ configuration }} -o {{ publish_folder }}

# Check code formatting using CSharpier
lint-check:
    dotnet csharpier check .

# Fix code formatting issues using CSharpier
lint-fix:
    dotnet csharpier format .

# Open the solution in Visual Studio/ Registered application
vs:
    open {{solution}}
