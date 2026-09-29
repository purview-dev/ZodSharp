using ZodSharp.JsonSchema.SystemTextJson;

namespace ZodSharp.Json;

/// <summary>
/// Proves that <c>Purview.ZodSharp.SystemTextJson</c> and <c>Purview.ZodSharp.NewtonsoftJson</c> can be
/// referenced from the same project without <c>extern alias</c>: each package declares its JSON Schema
/// import types in a package-specific namespace, so the previously identical full names are now distinct.
/// Import exactly one JSON integration namespace per file.
/// </summary>
public class SystemTextJsonImportTests
{
	const string ObjectSchema =
		/*lang=json,strict*/
		"""
			{
				"type": "object",
				"properties": { "name": { "type": "string", "minLength": 1 } },
				"required": ["name"]
			}
			""";

	[Test]
	public async Task FromJsonSchema_GivenSystemTextJsonPackage_ValidatesData()
	{
		// Arrange
		var schema = Z.FromJsonSchema(ObjectSchema);

		// Act
		var validResult = schema.Validate(new Dictionary<string, object?> { ["name"] = "Ada" });
		var invalidResult = schema.Validate(new Dictionary<string, object?> { ["name"] = "" });

		// Assert
		await Assert.That(validResult.IsSuccess).IsTrue();
		await Assert.That(invalidResult.IsSuccess).IsFalse();
	}

	[Test]
	public async Task ImportTypes_GivenBothJsonPackagesReferenced_ExposeDistinctFullNames()
	{
		// Arrange
		var systemTextJsonParser = typeof(FromJsonSchemaParser);
		var newtonsoftParser = typeof(ZodSharp.JsonSchema.NewtonsoftJson.FromJsonSchemaParser);

		// Act
		var systemTextJsonFullName = systemTextJsonParser.FullName;
		var newtonsoftFullName = newtonsoftParser.FullName;

		// Assert
		await Assert.That(systemTextJsonFullName).IsEqualTo("ZodSharp.JsonSchema.SystemTextJson.FromJsonSchemaParser");
		await Assert.That(newtonsoftFullName).IsEqualTo("ZodSharp.JsonSchema.NewtonsoftJson.FromJsonSchemaParser");
		await Assert.That(systemTextJsonParser).IsNotEqualTo(newtonsoftParser);
	}

	[Test]
	public async Task JsonSchemaSerializerOptions_GivenBothJsonPackagesReferenced_BindToOwnSerializerType()
	{
		// Arrange
		var systemTextJsonOptions = JsonSchemaSerializerOptions.Default;
		var newtonsoftOptions = ZodSharp.JsonSchema.NewtonsoftJson.JsonSchemaSerializerOptions.Default;

		// Act
		var systemTextJsonOptionsType = systemTextJsonOptions.GetType();
		var newtonsoftOptionsType = newtonsoftOptions.GetType();

		// Assert
		await Assert.That(systemTextJsonOptionsType).IsEqualTo(typeof(System.Text.Json.JsonSerializerOptions));
		await Assert.That(newtonsoftOptionsType).IsEqualTo(typeof(Newtonsoft.Json.JsonSerializerSettings));
	}
}
