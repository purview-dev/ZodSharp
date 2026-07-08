using System.Collections.Immutable;
using System.Text;

namespace ZodSharp.SourceGenerators.Helpers;

sealed class CodeWriter
{
	static readonly ImmutableDictionary<int, string> IndentCache = CreateIndentCache();

	readonly StringBuilder _builder = new();
	int _indentLevel;

	public CodeWriter Indent()
	{
		_indentLevel++;

		return this;
	}

	public CodeWriter Unindent()
	{
		if (_indentLevel == 0)
			throw new InvalidOperationException("Cannot unindent below zero.");

		_indentLevel--;

		return this;
	}

	public CodeWriter NewLine()
	{
		_builder.AppendLine();

		return this;
	}

	public CodeWriter WriteLine(string? value = null)
	{
		WriteIndent();
		_builder.AppendLine(value);

		return this;
	}

	public CodeWriter WriteIndent()
	{
		_builder.Append(IndentCache[_indentLevel]);
		return this;
	}

	public CodeWriter Write(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentNullException(nameof(value));

		_builder.Append(value);

		return this;
	}

	public CodeWriter Quote(string? value = null)
	{
		Write("\"");
		if (!string.IsNullOrEmpty(value))
			Write(value!);

		return Write("\"");
	}

	public CodeWriter QuoteLine(string? value = null) => Quote(value).WriteLine();

	public IDisposable Block(string? header = null)
	{
		if (header != null)
			WriteLine(header);

		WriteLine("{");
		Indent();

		return new BlockScope(this);
	}

	public override string ToString() => _builder.ToString();

	static ImmutableDictionary<int, string> CreateIndentCache() =>
		Enumerable.Range(0, 7).Select(i => new KeyValuePair<int, string>(i, new('\t', i))).ToImmutableDictionary();

	sealed class BlockScope(CodeWriter writer) : IDisposable
	{
		bool _disposed;

		public void Dispose()
		{
			if (_disposed)
				return;

			writer.Unindent();
			writer.WriteLine("}");

			_disposed = true;
		}
	}
}
