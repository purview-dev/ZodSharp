namespace Purview.SourceGenerators.Testing.TUnit;

public class SmokeTests
{
	[Test]
	public async Task AdapterCanBeConstructed()
	{
		var output = new TUnitTestOutput();
		output.WriteLine("adapter constructed");

		await Assert.That(output.GetType().Name).IsEqualTo(nameof(TUnitTestOutput));
	}
}
