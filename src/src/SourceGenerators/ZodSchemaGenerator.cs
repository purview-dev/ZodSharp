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
		context
			.RegisterEmbeddedAttribute<ZodSchemaGenerator>()
			.RegisterPostInitializationOutput(static ctx =>
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
				if (model.Context.Settings.IsSourceGeneratorDisabled)
					return;

				foreach (var schema in model.ZodSchemas)
				{
					if (schema.HasDiagnostics)
						spc.ReportDiagnostics(schema.Diagnostics);

					if (!schema.ShouldProcess)
						continue;

					var symbol = SymbolResolver.Resolve(
						model.Context.Capabilities.Compilation,
						schema.Value.SchemaType
					);
					if (symbol is null)
						continue;

					SchemaGenerationOutputContext outputContext = new(model.Context);
					BuildSchema(symbol, schema.Value.SchemaType, outputContext, spc, schema.Value.IsPrimary);
				}
			}
		);
	}
}
