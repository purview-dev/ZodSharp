using ZodSharp.Core;

namespace ZodSharp.SourceGenerators.Infra;

public sealed record ValidationRuleConventionsAnalyzerTestOptions : AnalyzerTestOptions
{
	public ValidationRuleConventionsAnalyzerTestOptions()
	{
		AdditionalNamespaces = ["ZodSharp.Core"];
		AdditionalAssemblyTypes = [typeof(IValidationRule<>)];
	}
}
