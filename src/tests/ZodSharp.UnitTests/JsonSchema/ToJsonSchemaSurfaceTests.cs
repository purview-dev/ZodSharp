namespace ZodSharp.JsonSchema;

/// <summary>
/// Breadth coverage for JSON Schema export.
/// </summary>
/// <remarks>
/// The exporter reached into schema internals by field name. Several of those names did not match any
/// declared field, and because <c>GetField</c> returns null rather than throwing, the affected constraints
/// were dropped silently. These tests assert the exported shape for each construct so a missing constraint
/// fails loudly instead.
/// </remarks>
public class ToJsonSchemaSurfaceTests
{
	[Test]
	public async Task ToJsonSchema_GivenStringConstraints_ExportsThem()
	{
		var jsonSchema = Z.ToJsonSchema(Z.String().Min(2).Max(10));

		await Assert.That(jsonSchema.Type).IsEqualTo("string");
		await Assert.That(jsonSchema.MinLength).IsEqualTo(2);
		await Assert.That(jsonSchema.MaxLength).IsEqualTo(10);
	}

	[Test]
	public async Task ToJsonSchema_GivenEmailString_ExportsFormat()
	{
		var jsonSchema = Z.ToJsonSchema(Z.String().Email());

		await Assert.That(jsonSchema.Format).IsEqualTo("email");
	}

	[Test]
	public async Task ToJsonSchema_GivenRegexString_ExportsPattern()
	{
		var jsonSchema = Z.ToJsonSchema(Z.String().Regex("^[a-z]+$"));

		await Assert.That(jsonSchema.Pattern).IsEqualTo("^[a-z]+$");
	}

	[Test]
	public async Task ToJsonSchema_GivenNumberConstraints_ExportsThem()
	{
		var jsonSchema = Z.ToJsonSchema(Z.Number().Min(1).Max(99));

		await Assert.That(jsonSchema.Type).IsEqualTo("number");
		await Assert.That(jsonSchema.Minimum).IsEqualTo(1);
		await Assert.That(jsonSchema.Maximum).IsEqualTo(99);
	}

	[Test]
	public async Task ToJsonSchema_GivenMultipleOf_ExportsIt()
	{
		var jsonSchema = Z.ToJsonSchema(Z.Number().MultipleOf(5));

		await Assert.That(jsonSchema.MultipleOf).IsEqualTo(5);
	}

	[Test]
	public async Task ToJsonSchema_GivenObject_ExportsProperties()
	{
		var schema = Z.Object().Field("name", Z.String().Min(1)).Field("age", Z.Number().Min(0)).Build();

		var jsonSchema = Z.ToJsonSchema(schema);

		await Assert.That(jsonSchema.Type).IsEqualTo("object");
		await Assert.That(jsonSchema.Properties).IsNotNull();
		await Assert.That(jsonSchema.Properties!.ContainsKey("name")).IsTrue();
		await Assert.That(jsonSchema.Properties!.ContainsKey("age")).IsTrue();
		await Assert.That(jsonSchema.Properties!["name"].Type).IsEqualTo("string");
		await Assert.That(jsonSchema.Properties!["age"].Type).IsEqualTo("number");
	}

	[Test]
	public async Task ToJsonSchema_GivenNestedObject_ExportsNestedProperties()
	{
		var inner = Z.Object().Field("city", Z.String()).Build();
		var schema = Z.Object().Field("address", inner).Build();

		var jsonSchema = Z.ToJsonSchema(schema);

		await Assert.That(jsonSchema.Properties!.ContainsKey("address")).IsTrue();
		await Assert.That(jsonSchema.Properties!["address"].Type).IsEqualTo("object");
		await Assert.That(jsonSchema.Properties!["address"].Properties!.ContainsKey("city")).IsTrue();
	}

	[Test]
	public async Task ToJsonSchema_GivenOptionalField_DoesNotMarkItRequired()
	{
		// Arrange — the builders wrap each field schema to present it untyped, so asking the wrapper's type
		// name whether it is a ZodOptional sees the wrapper and marks every optional field required.
		var schema = Z.Object().Field("name", Z.String()).Field("nickname", Z.Optional(Z.String())).Build();

		// Act
		var jsonSchema = Z.ToJsonSchema(schema);

		// Assert
		await Assert.That(jsonSchema.Required).IsNotNull();
		await Assert.That(jsonSchema.Required!).Contains("name");
		await Assert.That(jsonSchema.Required!.Contains("nickname")).IsFalse();
	}

	[Test]
	public async Task ToJsonSchema_GivenOptional_ExportsTheInnerSchemaShape()
	{
		// Arrange
		var schema = Z.Object().Field("nickname", Z.Optional(Z.String().Min(3))).Build();

		// Act
		var jsonSchema = Z.ToJsonSchema(schema);

		// Assert — the optional wrapper must not erase the inner schema's type or constraints.
		var nickname = jsonSchema.Properties!["nickname"];

		await Assert.That(nickname.Type).IsEqualTo("string");
		await Assert.That(nickname.MinLength).IsEqualTo(3);
	}

	[Test]
	public async Task ToJsonSchema_GivenLiteral_ExportsConst()
	{
		var jsonSchema = Z.ToJsonSchema(Z.Literal("fixed"));

		await Assert.That(jsonSchema.Const).IsEqualTo("fixed");
	}

	[Test]
	public async Task ToJsonSchema_GivenBoolean_ExportsType()
	{
		var jsonSchema = Z.ToJsonSchema(Z.Boolean());

		await Assert.That(jsonSchema.Type).IsEqualTo("boolean");
	}
}
