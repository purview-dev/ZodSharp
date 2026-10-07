using ZodSharp.CodeFixes;

namespace ZodSharp.SourceGenerators.Infra;

public abstract class ZodCodeFixTestBase
	: TUnitCodeFixTestBase<ZodSchemaAnalyzer, NativeUnionCodeFixProvider, ZodCodeFixTestOptions>
{
	// Empty
}
