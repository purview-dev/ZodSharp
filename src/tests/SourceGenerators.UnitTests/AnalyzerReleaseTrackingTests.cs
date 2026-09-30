using System.Text.RegularExpressions;

namespace ZodSharp.SourceGenerators;

/// <summary>
/// Guards the analyzer release tracking files: every diagnostic that ships in the stable 2.0.0 release is
/// recorded in <c>AnalyzerReleases.Shipped.md</c>, the unshipped file declares no new rules, and each rule
/// identifier is tracked exactly once. These files drive the Roslyn release-tracking analyzers
/// (<c>RS2000</c>/<c>RS2001</c>), which report at build time when a diagnostic is missing from either file.
/// </summary>
public partial class AnalyzerReleaseTrackingTests
{
	static readonly string ReleaseDirectory = Path.Combine(
		"..",
		"..",
		"..",
		"..",
		"..",
		"..",
		"src",
		"src",
		"SourceGenerators"
	);

	static readonly string[] ExpectedShippedRuleIds =
	[
		"ZODSGEN001",
		"ZODSGEN003",
		"ZODSGEN004",
		"ZODSGEN005",
		"ZODSGEN006",
		"ZODSGEN007",
		"ZODSGEN008",
		"ZODSGEN009",
		"ZODSGEN010",
		"ZODSGEN011",
		"ZODSGEN012",
		"ZODSGEN013",
		"ZODSGEN014",
		"ZODSGEN015",
		"ZODSGEN016",
		"ZODSGEN017",
		"ZODSGEN018",
		"ZODSGEN019",
		"ZODSGEN020",
		"ZODSGEN021",
		"ZODSGEN027",
		"ZODSGEN028",
		"ZODSGEN029",
		"ZODSGEN030",
		"ZODSGEN031",
		"ZODSGEN032",
		"ZODSGEN033",
		"ZODSGEN034",
		"ZODSGEN035",
		"ZODSGEN036",
		"ZODSASP001",
		"ZODSASP002",
		"ZODSASP003",
		"ZODSASP100",
		"ZODSASP101",
	];

	/// <summary>
	/// Rules added since the last stable release. Each identifier must also appear in
	/// <c>AnalyzerReleases.Unshipped.md</c> so the Roslyn release-tracking analyzers stay satisfied.
	/// </summary>
	static readonly string[] ExpectedUnshippedRuleIds = ["ZODSGEN037", "ZODSGEN038", "ZODSGEN039", "ZODSGEN040"];

	[Test]
	public async Task ShippedReleases_GivenStableRelease_DeclareRelease2_0_0WithEveryDiagnosticId(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var markdown = await ReadReleaseFileAsync("AnalyzerReleases.Shipped.md", cancellationToken);

		// Act
		var ruleIds = ReadRuleIds(markdown);

		// Assert
		await Assert.That(markdown).Contains("## Release 2.0.0");
		await Assert.That(SortedRuleIds(ruleIds)).IsEqualTo(SortedRuleIds(ExpectedShippedRuleIds));
	}

	[Test]
	public async Task UnshippedRelease_GivenRulesAddedSinceTheStableRelease_DeclaresOnlyThoseRules(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var markdown = await ReadReleaseFileAsync("AnalyzerReleases.Unshipped.md", cancellationToken);

		// Act
		var ruleIds = ReadRuleIds(markdown);

		// Assert
		await Assert.That(markdown).Contains("### New Rules");
		await Assert.That(markdown).Contains("; Unshipped analyzer release");
		await Assert.That(SortedRuleIds(ruleIds)).IsEqualTo(SortedRuleIds(ExpectedUnshippedRuleIds));
	}

	[Test]
	public async Task ReleaseFiles_GivenAllDiagnostics_TrackEachRuleIdExactlyOnce(CancellationToken cancellationToken)
	{
		// Arrange
		var shippedRuleIds = ReadRuleIds(await ReadReleaseFileAsync("AnalyzerReleases.Shipped.md", cancellationToken));
		var unshippedRuleIds = ReadRuleIds(
			await ReadReleaseFileAsync("AnalyzerReleases.Unshipped.md", cancellationToken)
		);

		// Act
		var duplicatedShippedRuleIds = shippedRuleIds
			.GroupBy(static id => id, StringComparer.Ordinal)
			.Where(static group => group.Count() > 1)
			.Select(static group => group.Key)
			.OrderBy(static id => id, StringComparer.Ordinal)
			.ToArray();
		var rulesInBothFiles = shippedRuleIds
			.Intersect(unshippedRuleIds, StringComparer.Ordinal)
			.OrderBy(static id => id, StringComparer.Ordinal)
			.ToArray();

		// Assert
		await Assert.That(string.Join(",", duplicatedShippedRuleIds)).IsEqualTo(string.Empty);
		await Assert.That(string.Join(",", rulesInBothFiles)).IsEqualTo(string.Empty);
		await Assert.That(shippedRuleIds.Length).IsEqualTo(ExpectedShippedRuleIds.Length);
		await Assert.That(unshippedRuleIds.Length).IsEqualTo(ExpectedUnshippedRuleIds.Length);
	}

	static async Task<string> ReadReleaseFileAsync(string fileName, CancellationToken cancellationToken) =>
		await File.ReadAllTextAsync(Path.Combine(ReleaseDirectory, fileName), cancellationToken);

	static string[] ReadRuleIds(string markdown) =>
		[.. RuleIdRegex().Matches(markdown).Select(static match => match.Groups[1].Value)];

	[GeneratedRegex(@"\|[ \t]*(ZODS[A-Z]{3}\d{3})[ \t]*\|")]
	private static partial Regex RuleIdRegex();

	static string SortedRuleIds(IEnumerable<string> ruleIds) =>
		string.Join(",", ruleIds.Distinct(StringComparer.Ordinal).OrderBy(static id => id, StringComparer.Ordinal));
}
