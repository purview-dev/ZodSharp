using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZodSharp.SourceGenerators.Models;

namespace ZodSharp.SourceGenerators.Helpers;

static class SourceGenLibrary
{
	public static IncrementalValueProvider<SchemaGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context
	)
	{
		var generationContext = IncrementalPipeline.GenerationContextValueProvider<
			SchemaGenerationCapabilities,
			ZodSchemaGenerator
		>(
			context,
			static (compilation, _, _, _) =>
				new(compilation)
				{
					HasRequiredAttribute = TypeHelpers.HasType(
						compilation,
						TypeLibrary.DataAnnotations.RequiredAttribute
					),
				},
			PropertyLibrary.DisableZodSharpSourceGeneratorProperty
		);
		var schemaSets = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.ZodSchemaAttribute,
			predicate: static (node, _) => node is TypeDeclarationSyntax,
			transform: static (attributeContext, cancellationToken) =>
				GetSchemasForGeneration(attributeContext, cancellationToken)
		);

		return generationContext.CollectWith(
			schemaSets,
			static (outputContext, sets, _) =>
				new SchemaGenerationModel(outputContext) { ZodSchemas = Deduplicate(sets) },
			"CollectZodSchemas"
		);
	}

	static GeneratorResult<SchemaSet> GetSchemasForGeneration(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		if (context.SemanticModel.GetDeclaredSymbol(context.TargetNode, cancellationToken) is not INamedTypeSymbol root)
			return GeneratorResult<SchemaSet>.Empty;

		var schemas = ImmutableArray.CreateBuilder<ZodSchemaDescriptor>();
		var seen = new HashSet<TypeIdentity>();
		var queue = new Queue<(INamedTypeSymbol Symbol, bool IsPrimary)>();
		queue.Enqueue((root, true));

		while (queue.Count > 0)
		{
			var (symbol, isPrimary) = queue.Dequeue();
			var identity = new TypeIdentity(symbol);
			if (!seen.Add(identity))
				continue;

			schemas.Add(new(identity, GetDeclarationFingerprint(symbol, cancellationToken), isPrimary));
			foreach (var property in GetZodProperties(symbol))
			{
				if (TryGetNestedSchemaType(property, out var nested))
					queue.Enqueue((nested, false));
			}
		}

		return GeneratorResult<SchemaSet>.Ok(new(schemas.ToImmutable()));
	}

	static EquatableArray<GeneratorResult<ZodSchemaDescriptor>> Deduplicate(
		ImmutableArray<GeneratorResult<SchemaSet>> sets
	)
	{
		var results = ImmutableArray.CreateBuilder<GeneratorResult<ZodSchemaDescriptor>>();
		var seen = new HashSet<TypeIdentity>();
		foreach (var set in sets)
		{
			if (!set.ShouldProcess)
				continue;

			foreach (var schema in set.Value.Schemas)
			{
				if (seen.Add(schema.SchemaType))
					results.Add(GeneratorResult<ZodSchemaDescriptor>.Ok(schema));
			}
		}

		return new(results.ToImmutable());
	}

	static string GetDeclarationFingerprint(INamedTypeSymbol symbol, CancellationToken cancellationToken) =>
		string.Join(
			"\n",
			symbol
				.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken).ToFullString())
				.OrderBy(static text => text, StringComparer.Ordinal)
		);

	static ImmutableArray<IPropertySymbol> GetZodProperties(INamedTypeSymbol symbol) =>
		[
			.. symbol
				.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(static property => property.DeclaredAccessibility == Accessibility.Public && !property.IsStatic),
		];

	static bool TryGetNestedSchemaType(IPropertySymbol property, out INamedTypeSymbol nested)
	{
		var propertyType = TypeHelpers.UnwrapNullableType(property.Type);
		if (propertyType is IArrayTypeSymbol array)
			propertyType = array.ElementType;
		else if (propertyType is INamedTypeSymbol named)
		{
			var enumerable = named.AllInterfaces.FirstOrDefault(TypeLibrary.Collections.IEnumerableT.Equals);
			if (enumerable is not null)
				propertyType = enumerable.TypeArguments[0];
		}

		propertyType = TypeHelpers.UnwrapNullableType(propertyType);
		nested = propertyType as INamedTypeSymbol ?? null!;
		return nested is not null
			&& nested.Locations.Any(static location => location.IsInSource)
			&& !ZodSchemaGenerator.IsScalarType(nested);
	}
}
