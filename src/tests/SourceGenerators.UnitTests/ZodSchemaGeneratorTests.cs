using ZodSharp.SourceGenerators.Infra;

namespace ZodSharp.SourceGenerators;

[Retry(3)]
public partial class ZodSchemaGeneratorTests : ZodSharpSourceGeneratorTestBase<ZodSchemaGenerator> { }
