using System.Collections.Concurrent;
using System.Globalization;

namespace ZodSharp.SourceGenerators.Helpers;

static class CodeGenHelpers
{
	public const string CodeGenReplacementToken = "//{{CodeGen}}";
	public const string AttribCodeGenReplacementToken = "//{{AttribCodeGen}}";
	public const string NonClassCodeGenReplacementToken = "//{{NonClassCodeGen}}";

	const string EmbedAttributesHashDefineName = "ZODSHARP_ATTRIBUTES";

	const string GeneratedCodeConstant =
		"System.CodeDom.Compiler.GeneratedCodeAttribute(\"{0}\", \"{1}\")";
	const string ConditionalConstant = "System.Diagnostics.ConditionalAttribute(\"{0}\")";
	const string CompilerGeneratedConstant = "System.Runtime.CompilerServices.CompilerGenerated";

	const string EmbeddedConstant = "Microsoft.CodeAnalysis.EmbeddedAttribute";
	const string ExcludeFromCodeCoverageConstant =
		"System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute";

	static readonly Lazy<string> GeneratedCodeAttribute = new(static () =>
		string.Format(
			CultureInfo.InvariantCulture,
			GeneratedCodeConstant,
			AssemblyInfo.RootNamespace,
			AssemblyInfo.Version
		)
	);

	static readonly Lazy<string> ConditionalAttribute = new(static () =>
		string.Format(
			CultureInfo.InvariantCulture,
			ConditionalConstant,
			EmbedAttributesHashDefineName
		)
	);

	static readonly Lazy<string[]> GenAttributes = new(static () =>
		[
			EmbeddedConstant,
			ExcludeFromCodeCoverageConstant,
			CompilerGeneratedConstant,
			GeneratedCodeAttribute.Value,
		]
	);

	static readonly Lazy<string[]> GenAttribAttributes = new(static () =>
		[
			EmbeddedConstant,
			ExcludeFromCodeCoverageConstant,
			ConditionalAttribute.Value,
			CompilerGeneratedConstant,
			GeneratedCodeAttribute.Value,
		]
	);

	static readonly Lazy<string[]> NonClassGenAttributes = new(static () =>
		[EmbeddedConstant, CompilerGeneratedConstant, GeneratedCodeAttribute.Value]
	);

	static readonly ConcurrentDictionary<int, string> GeneratedCodeAttributesByTabs = new();
	static readonly ConcurrentDictionary<int, string> AttributeGeneratedCodeAttributesByTabs =
		new();
	static readonly ConcurrentDictionary<int, string> NonClassGeneratedCodeAttributesByTabs = new();

	public const string NewLine = "\n";

	static string GlobalAttribute(string attribute) => $"[{Global(attribute)}]";

	public static string Global(this string type) => $"global::{type}";

	public static CodeWriter WriteRule(
		this CodeWriter writer,
		string propertyName,
		string comparison,
		string errorCode,
		string errorMessage
	)
	{
		using (writer.Block($"if ({comparison})"))
		{
			writer.WriteLine(
				$"errors ??= new {TypeLibrary.Collections.List.MakeGeneric(TypeLibrary.ValidationError)}();"
			);
			using (
				writer.Block(
					$"errors.Add(new {TypeLibrary.ValidationError}",
					separator: "(",
					closingSeparator: "));"
				)
			)
			{
				writer.WriteIndent().Quote(errorCode).WriteLine(",");
				writer.WriteIndent().Quote(errorMessage).WriteLine(",");
				writer.WriteLine($"new[] {{ \"{propertyName}\" }}");
			}
		}

		return writer.NewLine();
	}

	public static string GetGeneratedCodeAttribute(int tabs = 0) =>
		GeneratedCodeAttributesByTabs.GetOrAdd(
			tabs,
			static tabs =>
			{
				var t = string.Concat(Enumerable.Range(0, tabs).Select(static _ => '\t'));

				var result = string.Empty;
				foreach (var attr in GenAttributes.Value)
					result += $"{t}{GlobalAttribute(attr)}{NewLine}";

				return result;
			}
		);

	public static string GetAttributeGeneratedCodeAttribute(int tabs = 0) =>
		AttributeGeneratedCodeAttributesByTabs.GetOrAdd(
			tabs,
			static tabs =>
			{
				var t = string.Concat(Enumerable.Range(0, tabs).Select(static _ => '\t'));

				var result = string.Empty;
				foreach (var attr in GenAttribAttributes.Value)
					result += $"{t}{GlobalAttribute(attr)}{NewLine}";

				return result;
			}
		);

	public static string GetNonClassGeneratedCodeAttribute(int tabs = 0) =>
		NonClassGeneratedCodeAttributesByTabs.GetOrAdd(
			tabs,
			static tabs =>
			{
				var t = string.Concat(Enumerable.Range(0, tabs).Select(static _ => '\t'));

				var result = string.Empty;
				foreach (var attr in NonClassGenAttributes.Value)
					result += $"{t}{GlobalAttribute(attr)}{NewLine}";

				return result;
			}
		);

	public static string GetPathFieldName(string propertyName) => $"Path_{propertyName}";

	public static string GetLocalIdentifier(string propertyName, string suffix) =>
		string.IsNullOrEmpty(propertyName)
			? suffix
			: char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1) + suffix;

	public static string Quote(string value) =>
		$"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

	public static string QuoteChar(char value)
	{
		return value switch
		{
			'\'' => "'\\''",
			'\\' => "'\\\\'",
			'\0' => "'\\0'",
			'\a' => "'\\a'",
			'\b' => "'\\b'",
			'\f' => "'\\f'",
			'\n' => "'\\n'",
			'\r' => "'\\r'",
			'\t' => "'\\t'",
			'\v' => "'\\v'",
			_ => $"'{value}'",
		};
	}

	public static string ProcessGeneratedCode(CodeWriter writer) =>
		writer
			.ToString()
			.Replace(CodeGenReplacementToken, GetGeneratedCodeAttribute())
			.Replace(AttribCodeGenReplacementToken, GetAttributeGeneratedCodeAttribute())
			.Replace(NonClassCodeGenReplacementToken, GetNonClassGeneratedCodeAttribute());
}
