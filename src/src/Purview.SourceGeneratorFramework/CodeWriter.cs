using System.Text;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// A helper for writing indented C# source code.
/// </summary>
public sealed class CodeWriter
{
	const char IndentCharacter = '\t';
	static readonly Dictionary<int, string> IndentCache = new() { [0] = string.Empty };

	readonly StringBuilder _builder = new();
	int _indentLevel;
	bool _atLineStart = true;

	/// <summary>
	/// Increases the current indent level.
	/// </summary>
	public CodeWriter Indent()
	{
		_indentLevel++;

		return this;
	}

	/// <summary>
	/// Decreases the current indent level.
	/// </summary>
	public CodeWriter Unindent()
	{
		if (_indentLevel == 0)
		{
#if DEBUG
			return this;
#else
			throw new InvalidOperationException("Cannot unindent below zero.");
#endif
		}

		_indentLevel--;

		return this;
	}

	/// <summary>
	/// Appends a new line.
	/// </summary>
	public CodeWriter NewLine()
	{
		_builder.AppendLine();
		_atLineStart = true;

		return this;
	}

	/// <summary>
	/// Writes the specified value followed by a new line, applying the current indent.
	/// </summary>
	public CodeWriter WriteLine(string? value = null)
	{
		if (value is null)
			return NewLine();

		if (_atLineStart)
			WriteIndent();

		_builder.AppendLine(value);
		_atLineStart = true;

		return this;
	}

	/// <summary>
	/// Writes XML documentation lines.
	/// </summary>
	public CodeWriter WriteXml(params string[] xmlComment)
	{
		if (xmlComment == null || xmlComment.Length == 0)
			throw new ArgumentException("Xml comment cannot be null or empty.", nameof(xmlComment));

		foreach (var line in xmlComment)
			WriteLine($"/// {line}");

		return this;
	}

	/// <summary>
	/// Writes an XML summary documentation block.
	/// </summary>
	public CodeWriter WriteXmlSummary(params string[] summary)
	{
		if (summary == null || summary.Length == 0)
			throw new ArgumentException("Summary cannot be null or empty.", nameof(summary));

		WriteLine("/// <summary>");
		foreach (var line in summary)
			WriteLine($"/// {line}");

		return WriteLine("/// </summary>");
	}

	/// <summary>
	/// Writes a comment block.
	/// </summary>
	public CodeWriter Comment(params string[] comments)
	{
		if (comments == null || comments.Length == 0)
			throw new ArgumentException("Comments cannot be null or empty.", nameof(comments));

		if (comments.Length == 1)
			return WriteLine($"// {comments[0]}");

		WriteLine("/*");
		foreach (var line in comments)
			WriteLine($" * {line}");

		return WriteLine(" */");
	}

	/// <summary>
	/// Writes the current indent without any content.
	/// </summary>
	public CodeWriter WriteIndent()
	{
		_builder.Append(GetIndent(_indentLevel));
		_atLineStart = false;
		return this;
	}

	/// <summary>
	/// Writes the specified value without a trailing new line.
	/// </summary>
	public CodeWriter Write(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return this;

		if (_atLineStart)
			WriteIndent();

		_builder.Append(value);
		_atLineStart = false;

		return this;
	}

	/// <summary>
	/// Writes the specified character without a trailing new line.
	/// </summary>
	public CodeWriter Write(char value)
	{
		if (_atLineStart)
			WriteIndent();

		_builder.Append(value);
		_atLineStart = false;

		return this;
	}

	/// <summary>
	/// Writes a double-quoted string.
	/// </summary>
	public CodeWriter Quote(string? value = null)
	{
		Write("\"");
		if (!string.IsNullOrEmpty(value))
			Write(value);

		return Write("\"");
	}

	/// <summary>
	/// Writes a double-quoted string followed by a new line.
	/// </summary>
	public CodeWriter QuoteLine(string? value = null) => Quote(value).WriteLine();

	/// <summary>
	/// Starts a scoped block and returns an <see cref="IDisposable"/> that closes it.
	/// </summary>
	public IDisposable Block(
		string? header = null,
		string? separator = "{",
		string? closingSeparator = null,
		Action<CodeWriter>? additionalParts = null
	)
	{
		if (header != null)
		{
			if (_atLineStart)
				WriteIndent();

			Write(header);
			additionalParts?.Invoke(this);
			if (!_atLineStart)
				NewLine();
		}

		if (separator != null)
			WriteLine(separator);

		Indent();

		closingSeparator ??= GetDefaultClosingToken(separator);

		return new BlockScope(this, closingSeparator);
	}

	/// <summary>
	/// Writes each part on its own line.
	/// </summary>
	public CodeWriter MultiLine(params string[] parts)
	{
		if (parts == null)
			throw new ArgumentNullException(nameof(parts));

		foreach (var part in parts)
			WriteLine(part);

		return this;
	}

	/// <summary>
	/// Writes a multi-line parameter list ending with a closing parenthesis.
	/// </summary>
	public CodeWriter MultiLineParameters(params string[] parameters)
	{
		if (parameters == null)
			throw new ArgumentNullException(nameof(parameters));
		if (parameters.Length == 0)
		{
			Write(")");
			return this;
		}

		NewLine();
		Indent();
		for (var i = 0; i < parameters.Length; i++)
		{
			var isLast = i == parameters.Length - 1;
			WriteLine(isLast ? $"{parameters[i]})" : $"{parameters[i]},");
		}

		Unindent();

		return this;
	}

	/// <summary>
	/// Writes a comma-separated list of items, one per line.
	/// </summary>
	public CodeWriter MultiLineItems(params string[] items)
	{
		if (items == null)
			throw new ArgumentNullException(nameof(items));

		for (var i = 0; i < items.Length; i++)
		{
			var isLast = i == items.Length - 1;
			WriteLine(isLast ? items[i] : $"{items[i]},");
		}

		return this;
	}

	/// <summary>
	/// Increases the indent for the lifetime of the returned scope.
	/// </summary>
	public IDisposable Indented()
	{
		Indent();
		return new IndentScope(this);
	}

	/// <summary>
	/// Writes the supplied line and then increases the indent for the lifetime of the returned scope.
	/// </summary>
	public IDisposable Indented(string line)
	{
		WriteLine(line);
		return Indented();
	}

	/// <summary>
	/// Resets the writer to an empty state.
	/// </summary>
	public IDisposable Begin()
	{
		Reset();
		return new NoopScope();
	}

	/// <summary>
	/// Returns the generated source text.
	/// </summary>
	public override string ToString() => _builder.ToString();

	void Reset()
	{
		_builder.Clear();
		_indentLevel = 0;
		_atLineStart = true;
	}

	static string GetIndent(int indentLevel)
	{
		if (!IndentCache.TryGetValue(indentLevel, out var indent))
		{
			indent = new string(IndentCharacter, indentLevel);
			IndentCache[indentLevel] = indent;
		}

		return indent;
	}

	static string? GetDefaultClosingToken(string? openingToken)
	{
		return openingToken switch
		{
			"{" => "}",
			"(" => ")",
			"[" => "]",
			_ => null,
		};
	}

	sealed class NoopScope : IDisposable
	{
		public void Dispose() { }
	}

	sealed class IndentScope(CodeWriter writer) : IDisposable
	{
		bool _disposed;

		public void Dispose()
		{
			if (_disposed)
				return;

			writer.Unindent();

			_disposed = true;
		}
	}

	sealed class BlockScope(CodeWriter writer, string? closingSeperator) : IDisposable
	{
		bool _disposed;

		public void Dispose()
		{
			if (_disposed)
				return;

			writer.Unindent();
			writer.WriteLine(closingSeperator);

			_disposed = true;
		}
	}
}
