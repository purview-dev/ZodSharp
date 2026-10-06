namespace ZodSharp.JsonSchema;

/// <summary>
/// Regression tests for array length constraints in exported JSON Schema.
/// </summary>
/// <remarks>
/// The converter used to look for rules named <c>MinItemsRule</c>/<c>MaxItemsRule</c> and read private fields
/// called <c>_minItems</c>/<c>_maxItems</c> from them. Neither type nor field has ever existed —
/// <see cref="Schemas.ZodArray{T}"/> keeps its bounds in its own state — so <c>GetField</c> always returned
/// null and the constraints were silently dropped from the exported schema. Name-based reflection into the
/// library's own private state fails exactly this quietly, which is why the converter no longer uses it.
/// </remarks>
public class ToJsonSchemaArrayConstraintTests
{
	[Test]
	public async Task ToJsonSchema_GivenArrayWithMin_ExportsMinItems()
	{
		// Arrange
		var schema = Z.Array(Z.Number()).Min(2);

		// Act
		var jsonSchema = Z.ToJsonSchema(schema);

		// Assert
		await Assert.That(jsonSchema.Type).IsEqualTo("array");
		await Assert.That(jsonSchema.MinItems).IsEqualTo(2);
	}

	[Test]
	public async Task ToJsonSchema_GivenArrayWithMax_ExportsMaxItems()
	{
		// Arrange
		var schema = Z.Array(Z.Number()).Max(5);

		// Act
		var jsonSchema = Z.ToJsonSchema(schema);

		// Assert
		await Assert.That(jsonSchema.MaxItems).IsEqualTo(5);
	}

	[Test]
	public async Task ToJsonSchema_GivenArrayWithMinAndMax_ExportsBoth()
	{
		// Arrange
		var schema = Z.Array(Z.String()).Min(1).Max(3);

		// Act
		var jsonSchema = Z.ToJsonSchema(schema);

		// Assert
		await Assert.That(jsonSchema.MinItems).IsEqualTo(1);
		await Assert.That(jsonSchema.MaxItems).IsEqualTo(3);
	}

	[Test]
	public async Task ToJsonSchema_GivenArrayWithLength_ExportsBothBounds()
	{
		// Arrange — Length sets both bounds to the same value.
		var schema = Z.Array(Z.Number()).Length(4);

		// Act
		var jsonSchema = Z.ToJsonSchema(schema);

		// Assert
		await Assert.That(jsonSchema.MinItems).IsEqualTo(4);
		await Assert.That(jsonSchema.MaxItems).IsEqualTo(4);
	}

	[Test]
	public async Task ToJsonSchema_GivenArrayWithoutBounds_LeavesThemUnset()
	{
		// Arrange
		var schema = Z.Array(Z.Number());

		// Act
		var jsonSchema = Z.ToJsonSchema(schema);

		// Assert
		await Assert.That(jsonSchema.MinItems).IsNull();
		await Assert.That(jsonSchema.MaxItems).IsNull();
	}

	[Test]
	public async Task ToJsonSchema_GivenArrayOfObjects_StillExportsTheElementSchema()
	{
		// Arrange — the element schema must survive alongside the bounds.
		var element = Z.Object().Field("email", Z.String().Min(5)).Build();
		var schema = Z.Array(element).Min(1);

		// Act
		var jsonSchema = Z.ToJsonSchema(schema);

		// Assert
		await Assert.That(jsonSchema.MinItems).IsEqualTo(1);
		await Assert.That(jsonSchema.Items).IsNotNull();
		await Assert.That(jsonSchema.Items!.Type).IsEqualTo("object");
	}
}
