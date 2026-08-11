namespace ZodSharp.Schemas;

public class ZodRecordTests
{
	[Test]
	public async Task Record_GivenAllValuesValid_ReturnsSuccess()
	{
		// Arrange
		var schema = Z.Record(Z.Number().Min(0));

		// Act
		var result = schema.Validate(
			new Dictionary<string, double>
			{
				["a"] = 1.0,
				["b"] = 2.0,
				["c"] = 3.0,
			}
		);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value!["a"]).IsEqualTo(1.0);
	}

	[Test]
	public async Task Record_GivenSomeValuesInvalid_ReturnsFailureWithPath()
	{
		// Arrange
		var schema = Z.Record(Z.Number().Min(0));

		// Act
		var result = schema.Validate(new Dictionary<string, double> { ["a"] = 1.0, ["b"] = -5.0 });

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Path).Contains("b");
	}

	[Test]
	public async Task Record_GivenNull_ReturnsFailure()
	{
		// Arrange
		var schema = Z.Record(Z.String());

		// Act
		var result = schema.Validate(null!);

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("invalid_type");
	}

	[Test]
	public async Task Record_GivenEmptyDictionary_ReturnsSuccess()
	{
		// Arrange
		var schema = Z.Record(Z.String());

		// Act
		var result = schema.Validate([]);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value!.Count).IsEqualTo(0);
	}
}
