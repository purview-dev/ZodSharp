using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZodSharp.Core;

namespace ZodSharp.Json;

public class SystemTextJsonExtensionsTests
{
	static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

	[Test]
	public async Task DeserializeAndValidate_GivenValidJson_ReturnsSuccess()
	{
		var schema = new TestUserSchema();
		var json = """{"Name":"John","Age":30}""";

		var result = schema.DeserializeAndValidate(json);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value!.Name).IsEqualTo("John");
		await Assert.That(result.Value.Age).IsEqualTo(30);
	}

	[Test]
	public async Task DeserializeAndValidate_GivenValidJson_WithCamelCaseOptions_ReturnsSuccess()
	{
		var schema = new TestUserSchema();
		var json = """{"name":"John","age":30}""";

		var result = schema.DeserializeAndValidate(json, CamelCase);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value!.Name).IsEqualTo("John");
	}

	[Test]
	public async Task DeserializeAndValidate_GivenMalformedJson_ReturnsJsonErrorFailure()
	{
		var schema = new TestUserSchema();

		var result = schema.DeserializeAndValidate("{bad json");

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("json_error");
	}

	[Test]
	public async Task DeserializeAndValidate_GivenSchemaInvalidJson_ReturnsValidationFailure()
	{
		var schema = new TestUserSchema();
		var json = """{"Name":"","Age":30}""";

		var result = schema.DeserializeAndValidate(json);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors.Any(e => e.Path.Contains("Name"))).IsTrue();
	}

	[Test]
	public async Task DeserializeAndValidate_GivenNullJson_ThrowsArgumentNullException()
	{
		var schema = new TestUserSchema();

		var exception = Assert.Throws<ArgumentNullException>(() => schema.DeserializeAndValidate(null!));

		await Assert.That(exception!.ParamName).IsEqualTo("json");
	}

	[Test]
	public async Task DeserializeAndValidate_GivenNullSchema_ThrowsArgumentNullException()
	{
		IZodSchema<TestUser, TestUser> schema = null!;

		var exception = Assert.Throws<ArgumentNullException>(() => schema.DeserializeAndValidate("{}"));

		await Assert.That(exception!.ParamName).IsEqualTo("schema");
	}

	[Test]
	public async Task DeserializeAndValidateAsync_GivenValidJsonStream_ReturnsSuccess()
	{
		var schema = new TestUserSchema();
		var jsonBytes = Encoding.UTF8.GetBytes("""{"Name":"John","Age":30}""");
		using var stream = new MemoryStream(jsonBytes);

		var result = await schema.DeserializeAndValidateAsync(stream);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value!.Name).IsEqualTo("John");
		await Assert.That(result.Value.Age).IsEqualTo(30);
	}

	[Test]
	public async Task DeserializeAndValidateAsync_GivenMalformedJsonStream_ReturnsJsonErrorFailure()
	{
		var schema = new TestUserSchema();
		var jsonBytes = Encoding.UTF8.GetBytes("{bad json");
		using var stream = new MemoryStream(jsonBytes);

		var result = await schema.DeserializeAndValidateAsync(stream);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("json_error");
	}

	[Test]
	public async Task ValidateAndSerialize_GivenValidObject_ReturnsSuccessWithJsonString()
	{
		var schema = new TestUserSchema();
		var user = new TestUser { Name = "John", Age = 30 };

		var result = schema.ValidateAndSerialize(user);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("""{"Name":"John","Age":30}""");
	}

	[Test]
	public async Task ValidateAndSerialize_GivenInvalidObject_ReturnsFailureWithoutSerializing()
	{
		var schema = new TestUserSchema();
		var user = new TestUser { Name = "", Age = 30 };

		var result = schema.ValidateAndSerialize(user);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors.Any(e => e.Path.Contains("Name"))).IsTrue();
	}

	[Test]
	public async Task ValidateAndSerialize_WithCamelCaseOptions_ProducesCamelCaseJson()
	{
		var schema = new TestUserSchema();
		var user = new TestUser { Name = "Jane", Age = 25 };

		var result = schema.ValidateAndSerialize(user, CamelCase);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("""{"name":"Jane","age":25}""");
	}

	[Test]
	public async Task ValidateAndSerialize_GivenNullValue_ReturnsFailure()
	{
		var schema = new TestUserSchema();

		var result = schema.ValidateAndSerialize((TestUser)null!);

		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task ValidateAndSerialize_GivenNullSchema_ThrowsArgumentNullException()
	{
		IZodSchema<TestUser, TestUser> schema = null!;

		var exception = Assert.Throws<ArgumentNullException>(() => schema.ValidateAndSerialize(new TestUser()));

		await Assert.That(exception!.ParamName).IsEqualTo("schema");
	}

	[Test]
	public async Task ValidateAndSerializeAsync_GivenValidObject_WritesJsonToStream()
	{
		var schema = new TestUserSchema();
		var user = new TestUser { Name = "John", Age = 30 };
		using var stream = new MemoryStream();

		var result = await schema.ValidateAndSerializeAsync(user, stream);

		await Assert.That(result.IsSuccess).IsTrue();
		stream.Position = 0;
		using var reader = new StreamReader(stream, Encoding.UTF8);
		var json = await reader.ReadToEndAsync();
		await Assert.That(json).Contains("John");
		await Assert.That(json).Contains("30");
	}

	[Test]
	public async Task ValidateAndSerializeAsync_GivenInvalidObject_ReturnsFailureWithoutWriting()
	{
		var schema = new TestUserSchema();
		var user = new TestUser { Name = "", Age = -1 };
		using var stream = new MemoryStream();

		var result = await schema.ValidateAndSerializeAsync(user, stream);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(stream.Length).IsEqualTo(0);
	}

	[Test]
	public async Task ValidateAndSerializeAsync_GivenNullStream_ThrowsArgumentNullException()
	{
		var schema = new TestUserSchema();
		var user = new TestUser { Name = "John", Age = 30 };

		var exception = Assert.Throws<ArgumentNullException>(() =>
			schema.ValidateAndSerializeAsync(user, null!).AsTask().GetAwaiter().GetResult()
		);

		await Assert.That(exception!.ParamName).IsEqualTo("output");
	}

	[Test]
	public async Task RoundTrip_SerializeThenDeserialize_ReturnsEquivalentObject()
	{
		var schema = new TestUserSchema();
		var original = new TestUser { Name = "Alice", Age = 42 };

		var serializeResult = schema.ValidateAndSerialize(original);
		await Assert.That(serializeResult.IsSuccess).IsTrue();

		var deserializeResult = schema.DeserializeAndValidate(serializeResult.Value!);
		await Assert.That(deserializeResult.IsSuccess).IsTrue();
		await Assert.That(deserializeResult.Value!.Name).IsEqualTo("Alice");
		await Assert.That(deserializeResult.Value.Age).IsEqualTo(42);
	}

	sealed class TestUser
	{
		public string? Name { get; set; }
		public int Age { get; set; }
	}

	sealed class TestUserSchema : IZodSchema<TestUser, TestUser>
	{
		public ValidationResult<TestUser> Validate(TestUser value)
		{
			if (value is null)
			{
				return ValidationResult<TestUser>.Failure(
					new ValidationError("invalid_type", "Expected user, but got null")
				);
			}

			List<ValidationError> errors = [];

			if (string.IsNullOrWhiteSpace(value.Name))
			{
				errors.Add(new ValidationError("too_small", "Name is required", [nameof(TestUser.Name)]));
			}

			if (value.Age < 0)
			{
				errors.Add(new ValidationError("too_small", "Age must be non-negative", [nameof(TestUser.Age)]));
			}

			return errors.Count == 0
				? ValidationResult<TestUser>.Success(value)
				: ValidationResult<TestUser>.Failure(errors);
		}

		public ValueTask<ValidationResult<TestUser>> ValidateAsync(
			TestUser value,
			CancellationToken cancellationToken = default
		) => new(Validate(value));
	}
}
