using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	/// <summary>
	/// Output-scoped context for schema emission. Created inside the source-output callback;
	/// it is not part of the incremental pipeline.
	/// </summary>
	sealed class SchemaGenerationOutputContext(
		GenerationContext<SchemaGenerationCapabilities> context,
		ZodSchemaDescriptor zodSchema
	)
	{
		public GenerationContext<SchemaGenerationCapabilities> Context { get; } = context;
		public ZodSchemaDescriptor ZodSchema { get; } = zodSchema;
	}
}
