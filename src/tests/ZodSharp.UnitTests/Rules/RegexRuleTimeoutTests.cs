using System.Text.RegularExpressions;
using ZodSharp.Rules;

namespace ZodSharp.Rules;

/// <summary>
/// Guards the ReDoS budget on regex patterns the library compiles itself.
/// </summary>
/// <remarks>
/// A pattern supplied as a string reaches the library from a <c>[Regex]</c> attribute, a fluent
/// call, or an imported JSON Schema, and is then run against untrusted input. These tests pin that
/// such a pattern is bounded by a match timeout and that exceeding it surfaces as a validation
/// failure rather than an exception escaping into the host.
/// </remarks>
public class RegexRuleTimeoutTests
{
	// Classic catastrophic backtracking: nested quantifiers over an overlapping character class,
	// against input that cannot match because of the trailing '!'.
	const string CatastrophicPattern = "^(a+)+$";
	const string NonMatchingInput = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!";

	[Test]
	public async Task StringConstructor_AppliesTheDefaultMatchTimeout()
	{
		// Arrange / Act
		var rule = new RegexRule(CatastrophicPattern);

		// Assert — the budget must be positive and finite. Regex.InfiniteMatchTimeout is -1ms, so
		// comparing against it directly would not express "bounded"; check it is not that value.
		await Assert.That(RegexRule.DefaultMatchTimeout).IsGreaterThan(TimeSpan.Zero);
		await Assert.That(RegexRule.DefaultMatchTimeout).IsNotEqualTo(Regex.InfiniteMatchTimeout);
		await Assert.That(rule.Code).IsEqualTo(RegexRule.ErrorCode);
	}

	[Test]
	public async Task IsValid_GivenCatastrophicPatternAndInput_ReturnsFalseWithoutHanging()
	{
		// Arrange
		var rule = new RegexRule(CatastrophicPattern);

		// Act — without a timeout this does not return in any practical time. With one it must
		// come back quickly, as a plain validation failure rather than a thrown exception.
		var started = System.Diagnostics.Stopwatch.StartNew();
		var isValid = rule.IsValid(NonMatchingInput);
		started.Stop();

		// Assert
		await Assert.That(isValid).IsFalse();
		await Assert.That(started.Elapsed).IsLessThan(TimeSpan.FromSeconds(5));
	}

	[Test]
	public async Task IsValidSpan_GivenCatastrophicPatternAndInput_ReturnsFalseWithoutHanging()
	{
		// Arrange
		var rule = new RegexRule(CatastrophicPattern);

		// Act
		var started = System.Diagnostics.Stopwatch.StartNew();
		var isValid = rule.IsValid(NonMatchingInput.AsSpan());
		started.Stop();

		// Assert
		await Assert.That(isValid).IsFalse();
		await Assert.That(started.Elapsed).IsLessThan(TimeSpan.FromSeconds(5));
	}

	[Test]
	public async Task IsValid_GivenWellBehavedPattern_StillMatches()
	{
		// Arrange — the timeout must not change ordinary behaviour.
		var rule = new RegexRule("^[a-z]+$");

		// Act / Assert
		await Assert.That(rule.IsValid("abc")).IsTrue();
		await Assert.That(rule.IsValid("ABC")).IsFalse();
	}

	[Test]
	public async Task RegexConstructor_LeavesTheCallerSuppliedTimeoutAlone()
	{
		// Arrange — supplying a Regex is the documented escape hatch for a different budget.
		var supplied = new Regex("^[a-z]+$", RegexOptions.None, TimeSpan.FromSeconds(1));
		var rule = new RegexRule(supplied);

		// Act / Assert
		await Assert.That(rule.IsValid("abc")).IsTrue();
	}
}
