using Purview.SourceGeneratorFramework.Testing.Generators;

namespace ZodSharp.SourceGenerators.Models;

[GenerateAttributeDataModel("ZodSharp.ZodSchemaAttribute")]
readonly partial record struct ZodSchemaAttributeData(
	string? SchemaName,
	bool GenerateValidateMethod,
	bool GenerateParseMethod,
	bool EnableComposition,
	string? CustomValidationMethodName
);
