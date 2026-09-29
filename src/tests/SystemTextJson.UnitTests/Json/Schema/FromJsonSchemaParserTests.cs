using ZodSharp.JsonSchema;
using ZodSharp.JsonSchema.SystemTextJson;

namespace ZodSharp.Json.Schema;

/// <summary>
/// Covers JSON Schema import through the System.Text.Json package: the <c>Z.FromJsonSchema</c> extension
/// member, local and external <c>$ref</c> handling, and the exported serializer options.
/// </summary>
public class FromJsonSchemaParserTests
{
	const string LocalRefSchema =
		/*lang=json,strict*/
		"""
			{
				"$defs": { "name": { "type": "string", "minLength": 1 } },
				"type": "object",
				"properties": { "name": { "$ref": "#/$defs/name" } },
				"required": ["name"]
			}
			""";

	const string ExternalRefSchema =
		/*lang=json,strict*/
		"""
			{
				"type": "object",
				"properties": { "name": { "$ref": "external.json#/$defs/name" } }
			}
			""";

	[Test]
	public async Task FromJsonSchema_GivenLocalDefsRef_ResolvesReference()
	{
		// Arrange
		var schema = Z.FromJsonSchema(LocalRefSchema);

		// Act
		var validResult = schema.Validate(new Dictionary<string, object?> { ["name"] = "Ada" });
		var invalidResult = schema.Validate(new Dictionary<string, object?> { ["name"] = "" });

		// Assert
		await Assert.That(validResult.IsSuccess).IsTrue();
		await Assert.That(invalidResult.IsSuccess).IsFalse();
	}

	[Test]
	public async Task FromJsonSchema_GivenExternalRef_ThrowsNotSupportedExceptionNamingReference()
	{
		// Arrange

		// Act
		var exception = Assert.Throws<NotSupportedException>(() => Z.FromJsonSchema(ExternalRefSchema));

		// Assert
		await Assert.That(exception!.Message).Contains("External $ref 'external.json#/$defs/name' is not supported");
		await Assert.That(exception.Message).Contains("pre-resolve external schemas before importing");
	}

	[Test]
	public async Task Parse_GivenDefinitionWithStringConstraint_ValidatesAgainstConstraint()
	{
		// Arrange
		JsonSchemaDefinition definition = new() { Type = "string", MinLength = 2 };

		// Act
		var schema = FromJsonSchemaParser.Parse(definition);

		// Assert
		await Assert.That(schema.Validate("ab").IsSuccess).IsTrue();
		await Assert.That(schema.Validate("a").IsSuccess).IsFalse();
	}

	[Test]
	public async Task Parse_GivenNullDefinition_ThrowsArgumentNullException()
	{
		// Arrange
		JsonSchemaDefinition definition = null!;

		// Act
		var exception = Assert.Throws<ArgumentNullException>(() => FromJsonSchemaParser.Parse(definition));

		// Assert
		await Assert.That(exception!.ParamName).IsEqualTo("schema");
	}

	[Test]
	public async Task JsonSchemaSerializerOptions_Default_UsesCamelCasePropertyNames()
	{
		// Arrange

		// Act
		var options = JsonSchemaSerializerOptions.Default;

		// Assert
		await Assert.That(options.PropertyNamingPolicy).IsEqualTo(System.Text.Json.JsonNamingPolicy.CamelCase);
	}

	[Test]
	public async Task JsonSchemaSerializerOptions_GivenRefAndDefs_UsesJsonSchemaKeywordNames()
	{
		// Arrange
		JsonSchemaDefinition definition = new()
		{
			Ref = "#/$defs/name",
			Defs = new Dictionary<string, JsonSchemaDefinition> { ["name"] = new() { Type = "string" } },
		};

		// Act
		var json = System.Text.Json.JsonSerializer.Serialize(definition, JsonSchemaSerializerOptions.Default);

		// Assert
		await Assert.That(json).Contains("\"$ref\"");
		await Assert.That(json).Contains("\"$defs\"");
	}
}
