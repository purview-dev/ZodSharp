using System.Reflection;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGeneratorTests
{
	[Test]
	public async Task GeneratedValidate_GivenUserWithValidData_ReturnsSuccess(CancellationToken cancellationToken)
	{
		var assembly = await CompileToAssemblyAsync(UserSource, cancellationToken);
		var user = CreateUser(assembly, "John Doe", 30, "john@example.com");

		var result = InvokeValidate(assembly, user);

		await Assert.That((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).IsTrue();
	}

	[Test]
	[Arguments(null, 30, "john@example.com", "Name")]
	[Arguments("AB", 30, "john@example.com", "Name")]
	[Arguments(
		"This name is far too long for the generated StringLength validation rule",
		30,
		"john@example.com",
		"Name"
	)]
	[Arguments("John Doe", -1, "john@example.com", "Age")]
	[Arguments("John Doe", 121, "john@example.com", "Age")]
	[Arguments("John Doe", 30, "not-an-email", "Email")]
	public async Task GeneratedValidate_GivenInvalidUser_ReturnsFailureWithExpectedPath(
		string? name,
		int age,
		string? email,
		string expectedPath,
		CancellationToken cancellationToken
	)
	{
		var assembly = await CompileToAssemblyAsync(UserSource, cancellationToken);
		var user = CreateUser(assembly, name, age, email);

		var result = InvokeValidate(assembly, user);
		var errors = (System.Collections.Immutable.ImmutableArray<ZodSharp.Core.ValidationError>)
			result.GetType().GetProperty("Errors")!.GetValue(result)!;

		await Assert.That((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).IsFalse();
		await Assert.That(errors.Any(error => error.Path.Contains(expectedPath))).IsTrue();
	}

	[Test]
	public async Task GeneratedParse_GivenInvalidUser_ThrowsZodException(CancellationToken cancellationToken)
	{
		var assembly = await CompileToAssemblyAsync(UserSource, cancellationToken);
		var user = CreateUser(assembly, "AB", 30, "john@example.com");
		var schemaType = assembly.GetType("Testing.UserSchema")!;

		var exception = Assert.Throws<TargetInvocationException>(() =>
			schemaType.GetMethod("Parse")!.Invoke(null, [user])
		);

		await Assert.That(exception).IsNotNull();
		await Assert.That(exception!.InnerException).IsTypeOf<ZodSharp.Core.ZodException>();
	}

	[Test]
	public async Task Generate_GivenDataAnnotations_GeneratedSourceContainsExpectedValidationChecks(
		CancellationToken cancellationToken
	)
	{
		var (result, outputCompilation) = await GenerateAsync(UserSource, cancellationToken);
		var generatedSource = GetSchemaGeneratedSource(result, "UserSchema");

		await AssertNoGeneratorExceptions(result);
		await AssertNoCompilationErrors(outputCompilation, cancellationToken);
		await Assert.That(generatedSource).Contains("value.Name == null");
		await Assert.That(generatedSource).Contains("value.Name.Length < 3");
		await Assert.That(generatedSource).Contains("value.Name.Length > 50");
		await Assert.That(generatedSource).Contains("value.Age < 0");
		await Assert.That(generatedSource).Contains("value.Age > 120");
		await Assert.That(generatedSource).Contains("EmailRegex.IsMatch(value.Email)");
	}

	const string UserSource =
		@"
using System.ComponentModel.DataAnnotations;

namespace Testing
{
	[ZodSchema]
	public sealed class User
	{
		[Required]
		[StringLength(50, MinimumLength = 3)]
		public string? Name { get; set; }

		[Range(0, 120)]
		public int Age { get; set; }

		[EmailAddress]
		public string? Email { get; set; }
	}
}
";

	static object CreateUser(Assembly assembly, string? name, int age, string? email)
	{
		var userType = assembly.GetType("Testing.User")!;
		var user = Activator.CreateInstance(userType)!;
		userType.GetProperty("Name")!.SetValue(user, name);
		userType.GetProperty("Age")!.SetValue(user, age);
		userType.GetProperty("Email")!.SetValue(user, email);
		return user;
	}

	static object InvokeValidate(Assembly assembly, object user)
	{
		var schemaType = assembly.GetType("Testing.UserSchema")!;
		return schemaType.GetMethod("Validate")!.Invoke(null, [user])!;
	}
}
