#if NET11_0_OR_GREATER
using ZodSharp.SourceGenerators.Infra;

namespace ZodSharp.SourceGenerators;

public class NativeUnionCodeFixTests : ZodCodeFixTestBase
{
	[Test]
	public async Task CodeFix_GivenZUnion_ReplacesWithNativeUnion(CancellationToken cancellationToken)
	{
		const string source = """
			using ZodSharp;

			namespace Testing
			{
				public static class Schemas
				{
					public static void Build()
					{
						var schema = Z.Union(Z.String(), Z.Object().Build());
					}
				}
			}
			""";

		var result = await ApplyCodeFixAsync(source, new ZodCodeFixTestOptions(), cancellationToken);

		await Assert.That(result.FixedSource).Contains("Z.NativeUnion(");
		await Assert.That(result.FixedSource).DoesNotContain("Z.Union(");
	}

	[Test]
	public async Task CodeFix_GivenExplicitTypedUnionDeclaration_RetargetsDeclarationAndCall(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.Collections.Generic;
			using ZodSharp;
			using ZodSharp.Schemas;

			namespace Testing
			{
				public static class Schemas
				{
					public static void Build()
					{
						ZodTypedUnion<string, Dictionary<string, object?>> schema = Z.Union(Z.String(), Z.Object().Build());
					}
				}
			}
			""";

		var result = await ApplyCodeFixAsync(source, new ZodCodeFixTestOptions(), cancellationToken);

		await Assert.That(result.FixedSource).Contains("ZodTypedNativeUnion<string, Dictionary<string, object?>>");
		await Assert.That(result.FixedSource).Contains("= Z.NativeUnion(");
	}

	[Test]
	public async Task CodeFix_GivenNewZodTypedUnion_ReplacesWithNativeSchema(CancellationToken cancellationToken)
	{
		const string source = """
			using ZodSharp;
			using ZodSharp.Schemas;

			namespace Testing
			{
				public static class Schemas
				{
					public static void Build()
					{
						var schema = new ZodTypedUnion<string, System.Collections.Generic.Dictionary<string, object?>>(
							Z.String(),
							Z.Object().Build()
						);
					}
				}
			}
			""";

		var result = await ApplyCodeFixAsync(source, new ZodCodeFixTestOptions(), cancellationToken);

		await Assert.That(result.FixedSource).Contains("new ZodTypedNativeUnion<");
	}
}
#endif
