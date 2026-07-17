using Microsoft.CodeAnalysis;
using ZodSharp.SourceGenerators.Helpers;
using ZodSharp.SourceGenerators.Templates;

namespace ZodSharp.SourceGenerators;

/// <summary>
/// Source generator that creates optimized validators for classes marked with [ZodSchema].
/// Uses IIncrementalGenerator for better performance and incremental compilation support.
/// </summary>
[Generator]
public sealed partial class ZodSchemaGenerator : IIncrementalGenerator, ILogSupport
{
	GenerationLogger? _logger;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static ctx =>
		{
			// Adds the EmbeddedAttribute definition to the compilation if not already present
			// ensuring that internal generated code doesn't cause visibility issues in referenced
			// assemblies where InternalVisibleTo is set.
			ctx.AddEmbeddedAttributeDefinition();

			ctx.AddSource(
				$"{nameof(TypeHelpers.ZodSchemaAttribute)}.g.cs",
				EmbeddedResources.LoadTemplate(nameof(TypeHelpers.ZodSchemaAttribute))
			);
		});

		var generationValueProviders = SourceGenHelpers.GetGeneratorValueProviders(context, _logger);

		// Register source outputs
		context.RegisterSourceOutput(
			generationValueProviders,
			static (spc, source) =>
			{
				if (!source.IsSourceGeneratorEnabled)
					return;

				foreach (var schema in source.ZodSchemas)
				{
					if (schema.HasDiagnostics)
					{
						ReportDiagnostics(spc, schema.Diagnostics, source.GenerationContext.Logger);
					}

					if (schema.IsFatal)
						return;

					Execute(schema.Value!, source.GenerationContext, spc);
				}
			}
		);
	}
}
