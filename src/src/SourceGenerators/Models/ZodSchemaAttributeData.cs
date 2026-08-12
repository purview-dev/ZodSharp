using Purview.SourceGeneratorFramework.Testing.Generators;

namespace ZodSharp.SourceGenerators.Models;

[GenerateAttributeDataModel("ZodSharp.ZodSchemaAttribute")]
readonly partial record struct ZodSchemaAttributeData(
	[AttributeProperty] string? SchemaName,
	[AttributeProperty] bool GenerateValidateMethod,
	[AttributeProperty] bool GenerateParseMethod,
	[AttributeProperty] bool EnableComposition,
	[AttributeProperty] string? CustomValidationMethodName
);
