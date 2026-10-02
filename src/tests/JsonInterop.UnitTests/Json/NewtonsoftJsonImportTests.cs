using ZodSharp.JsonSchema.NewtonsoftJson;

namespace ZodSharp.Json;

/// <summary>
/// Newtonsoft.Json counterpart of <see cref="SystemTextJsonImportTests"/>: the Newtonsoft package's JSON
/// Schema import types are available from the same project as the System.Text.Json package's types.
/// </summary>
public class NewtonsoftJsonImportTests
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
	public async Task FromJsonSchema_GivenNewtonsoftJsonPackage_ValidatesData()
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
		var newtonsoftParser = typeof(FromJsonSchemaParser);
		var systemTextJsonParser = typeof(JsonSchema.SystemTextJson.FromJsonSchemaParser);

		// Act
		var newtonsoftFullName = newtonsoftParser.FullName;
		var systemTextJsonFullName = systemTextJsonParser.FullName;

		// Assert
		await Assert.That(newtonsoftFullName).IsEqualTo("ZodSharp.JsonSchema.NewtonsoftJson.FromJsonSchemaParser");
		await Assert.That(systemTextJsonFullName).IsEqualTo("ZodSharp.JsonSchema.SystemTextJson.FromJsonSchemaParser");
		await Assert.That(newtonsoftParser).IsNotEqualTo(systemTextJsonParser);
	}

	[Test]
	public async Task JsonSchemaSerializerOptions_GivenBothJsonPackagesReferenced_BindToOwnSerializerType()
	{
		// Arrange
		var newtonsoftOptions = JsonSchemaSerializerOptions.Default;
		var systemTextJsonOptions = JsonSchema.SystemTextJson.JsonSchemaSerializerOptions.Default;

		// Act
		var newtonsoftOptionsType = newtonsoftOptions.GetType();
		var systemTextJsonOptionsType = systemTextJsonOptions.GetType();

		// Assert
		await Assert.That(newtonsoftOptionsType).IsEqualTo(typeof(Newtonsoft.Json.JsonSerializerSettings));
		await Assert.That(systemTextJsonOptionsType).IsEqualTo(typeof(System.Text.Json.JsonSerializerOptions));
	}
}
