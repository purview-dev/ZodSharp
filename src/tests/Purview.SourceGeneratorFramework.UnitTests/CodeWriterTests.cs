namespace Purview.SourceGeneratorFramework.UnitTests;

public class CodeWriterTests
{
	[Test]
	public async Task WriteLine_AppendsLineWithIndent()
	{
		var writer = new CodeWriter();

		writer.WriteLine("public class C");
		using (writer.Block())
		{
			writer.WriteLine("public int P { get; set; }");
		}

		var result = writer.ToString();

		await Assert.That(result).Contains("public class C");
		await Assert.That(result).Contains("\tpublic int P { get; set; }");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task Quote_WrapsValueInDoubleQuotes()
	{
		var writer = new CodeWriter();

		writer.Quote("value");

		await Assert.That(writer.ToString()).IsEqualTo("\"value\"");
	}

	[Test]
	public async Task ToString_EmptyWriter_ReturnsEmpty()
	{
		var writer = new CodeWriter();

		await Assert.That(writer.ToString()).IsEmpty();
	}
}
