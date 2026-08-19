using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using ZodSharp.SourceGenerators.Helpers;

namespace ZodSharp.SourceGenerators.Infra;

public record class ZodSharpGeneratorTestOptions : SourceGeneratorTestOptions
{
	static readonly Type[] DefaultAssemblyTypes =
[
	typeof(Z),
		typeof(ImmutableArray),
		typeof(RequiredAttribute),
		typeof(System.Text.Json.JsonSerializer),
		typeof(System.Text.RegularExpressions.Regex),
	];

	public static readonly string[] GeneratedAttributes = ["EmbeddedAttribute.cs", "ZodSchemaAttribute.g.cs"];

	public ZodSharpGeneratorTestOptions()
	{
		IncludeDefaultNamespaces = true;
		ThrowOnGenerationException = true;
		DisableSourceGeneratorPropertyName = PropertyLibrary.DisableZodSharpSourceGeneratorProperty;
		CompileToAssembly = true;

		AdditionalNamespaces = [TypeLibrary.ZodSharpNamespace];
		AdditionalAssemblyTypes = ImmutableArray.Create(DefaultAssemblyTypes);
		ExcludeGeneratedSourceHintNames = [.. GeneratedAttributes];
	}
}
