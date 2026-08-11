using Purview.SourceGenerators.Testing;
using TUnit.Core;

namespace Purview.SourceGenerators.Testing.TUnit;

/// <summary>
/// Routes generator log output to the current TUnit test context.
/// </summary>
public sealed class TUnitTestOutput : ITestOutput
{
	/// <inheritdoc />
	public void WriteLine(string message)
	{
		TestContext.Current?.OutputWriter.WriteLine(message);
	}
}
