using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators;

/// <summary>
/// Source generator that creates optimized validators for classes marked with [ZodSchema].
/// Uses IIncrementalGenerator for better performance and incremental compilation support.
/// </summary>
[Generator]
public sealed partial class ZodSchemaGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterEmbeddedAttribute(AssemblyInfo.AssemblyName, AssemblyInfo.Version);

		context.RegisterPostInitializationOutput(static ctx =>
		{
			foreach (var (HintName, SourceText) in AttributeGenHelper.GenerateMarkers())
				ctx.AddSource($"{HintName}.g.cs", SourceText);
		});

		var generationValueProviders = SourceGenLibrary.GetGeneratorValueProviders(context);

		// Register source outputs
		context.RegisterSourceOutput(
			generationValueProviders,
			(spc, model) =>
			{
				if (model.GenerationContext.Settings.IsSourceGeneratorDisabled)
					return;

				var isFatal = false;
				foreach (var schema in model.ZodSchemas)
				{
					if (schema.HasDiagnostics)
					{
						ReportDiagnostics(spc, schema.Diagnostics, model.GenerationContext);
					}

					if (schema.IsFatal)
						isFatal = true;
				}

				if (isFatal)
					return;

				SchemaGenerationOutputContext outputContext = new(model.GenerationContext);
				var allSchemas = DiscoverAllSchemas(model.ZodSchemas);
				foreach (var (descriptor, isPrimary) in allSchemas)
				{
					// We'll create a new new code writer for each schema.
					outputContext.CreateCodeWriter();
					BuildSchema(descriptor, outputContext, spc, isPrimary);
				}
			}
		);
	}
}
