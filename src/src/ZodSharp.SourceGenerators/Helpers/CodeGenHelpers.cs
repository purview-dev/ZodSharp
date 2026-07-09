using System.Collections.Concurrent;
using System.Globalization;

namespace ZodSharp.SourceGenerators.Helpers;

static class CodeGenHelpers
{
	public const string CodeGenReplacementToken = "{{CodeGen}}";
	public const string AttribCodeGenReplacementToken = "{{AttribCodeGen}}";
	public const string NonClassCodeGenReplacementToken = "{{NonClassCodeGen}}";

	const string EmbedAttributesHashDefineName = "ZODSHARP_ATTRIBUTES";

	const string GeneratedCodeConstant = "System.CodeDom.Compiler.GeneratedCodeAttribute(\"{0}\", \"{1}\")";
	const string ConditionalConstant = "System.Diagnostics.ConditionalAttribute(\"{0}\")";
	const string CompilerGeneratedConstant = "System.Runtime.CompilerServices.CompilerGenerated";

	const string EmbeddedConstant = "Microsoft.CodeAnalysis.EmbeddedAttribute";
	const string ExcludeFromCodeCoverageConstant = "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute";

	static readonly Lazy<string> GeneratedCodeAttribute = new(() =>
		string.Format(
			CultureInfo.InvariantCulture,
			GeneratedCodeConstant,
			AssemblyInfo.RootNamespace,
			AssemblyInfo.Version
		)
	);

	static readonly Lazy<string> ConditionalAttribute = new(() =>
		string.Format(CultureInfo.InvariantCulture, ConditionalConstant, EmbedAttributesHashDefineName)
	);

	static readonly Lazy<string[]> GenAttributes = new(() =>
		[EmbeddedConstant, ExcludeFromCodeCoverageConstant, CompilerGeneratedConstant, GeneratedCodeAttribute.Value]
	);

	static readonly Lazy<string[]> GenAttribAttributes = new(() =>
		[
			EmbeddedConstant,
			ExcludeFromCodeCoverageConstant,
			ConditionalAttribute.Value,
			CompilerGeneratedConstant,
			GeneratedCodeAttribute.Value,
		]
	);

	static readonly Lazy<string[]> NonClassGenAttributes = new(() =>
		[EmbeddedConstant, CompilerGeneratedConstant, GeneratedCodeAttribute.Value]
	);

	static readonly ConcurrentDictionary<int, string> GeneratedCodeAttributesByTabs = new();
	static readonly ConcurrentDictionary<int, string> AttributeGeneratedCodeAttributesByTabs = new();
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
			using (
				writer.Block(
					$"errors.Add(new {TypeHelpers.ValidationError.Global()}",
					seperator: "(",
					closingSeperator: "));"
				)
			)
			{
				writer.WriteIndent().Quote(errorCode).Write(",").NewLine();
				writer.WriteIndent().Quote(errorMessage).Write(",").NewLine();
				writer.WriteLine($"new[] {{ \"{propertyName}\" }}");
			}
		}

		return writer.NewLine();
	}

	public static string GetGeneratedCodeAttribute(int tabs = 0) =>
		GeneratedCodeAttributesByTabs.GetOrAdd(
			tabs,
			tabs =>
			{
				var t = string.Concat(Enumerable.Range(0, tabs).Select(_ => '\t'));

				string result = string.Empty;
				foreach (var attr in GenAttributes.Value)
					result += $"{t}{GlobalAttribute(attr)}{NewLine}";

				return result;
			}
		);

	public static string GetAttributeGeneratedCodeAttribute(int tabs = 0) =>
		AttributeGeneratedCodeAttributesByTabs.GetOrAdd(
			tabs,
			tabs =>
			{
				var t = string.Concat(Enumerable.Range(0, tabs).Select(_ => '\t'));

				string result = string.Empty;
				foreach (var attr in GenAttribAttributes.Value)
					result += $"{t}{GlobalAttribute(attr)}{NewLine}";

				return result;
			}
		);

	public static string GetNonClassGeneratedCodeAttribute(int tabs = 0) =>
		NonClassGeneratedCodeAttributesByTabs.GetOrAdd(
			tabs,
			tabs =>
			{
				var t = string.Concat(Enumerable.Range(0, tabs).Select(_ => '\t'));

				string result = string.Empty;
				foreach (var attr in NonClassGenAttributes.Value)
					result += $"{t}{GlobalAttribute(attr)}{NewLine}";

				return result;
			}
		);

	public static string ProcessGeneratedCode(string source) =>
		source
			.Replace(CodeGenReplacementToken, GetGeneratedCodeAttribute())
			.Replace(AttribCodeGenReplacementToken, GetAttributeGeneratedCodeAttribute())
			.Replace(NonClassCodeGenReplacementToken, GetNonClassGeneratedCodeAttribute());
}
