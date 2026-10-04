using System.Globalization;
using System.Text;
using ZodSharp.Core;

namespace ZodSharp.Rules;

/// <summary>
/// Covers the inclusive comparison and parity rules added alongside the existing generic bound rules.
/// </summary>
public class NumericRulesTests
{
	[Test]
	[Arguments(3.0, true)]
	[Arguments(4.0, true)]
	[Arguments(2.9, false)]
	public async Task GreaterThanOrEqualRule_GivenValue_ComparesInclusively(double value, bool expected)
	{
		// Arrange
		GreaterThanOrEqualRule<double> rule = new(3.0);

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
		await Assert.That(rule.Code).IsEqualTo(GreaterThanOrEqualRule<double>.ErrorCode);
	}

	[Test]
	[Arguments(3, true)]
	[Arguments(4, true)]
	[Arguments(2, false)]
	public async Task GreaterThanOrEqualRule_GivenInteger_ComparesInclusively(int value, bool expected)
	{
		// Arrange - the rule is generic over any IComparable<T>, so it closes with int as well as double.
		GreaterThanOrEqualRule<int> rule = new(3);

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
	}

	[Test]
	public async Task GreaterThanOrEqualRule_GivenFailure_FormatsTheBoundAndValue()
	{
		// Arrange
		GreaterThanOrEqualRule<double> rule = new(3.0);

		// Act
		var message = rule.GetErrorMessage(1.0);
		var expected = string.Format(
			CultureInfo.CurrentCulture,
			CompositeFormat.Parse(GreaterThanOrEqualRule<double>.MessageFormat),
			3.0,
			1.0
		);

		// Assert
		await Assert.That(message).IsEqualTo(expected);
	}

	[Test]
	[Arguments(3.0, true)]
	[Arguments(2.0, true)]
	[Arguments(3.1, false)]
	public async Task LessThanOrEqualRule_GivenValue_ComparesInclusively(double value, bool expected)
	{
		// Arrange
		LessThanOrEqualRule<double> rule = new(3.0);

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
		await Assert.That(rule.Code).IsEqualTo(LessThanOrEqualRule<double>.ErrorCode);
	}

	[Test]
	[Arguments(3, true)]
	[Arguments(2, true)]
	[Arguments(4, false)]
	public async Task LessThanOrEqualRule_GivenInteger_ComparesInclusively(int value, bool expected)
	{
		// Arrange - the inclusive bound closes with the member's own numeric type.
		LessThanOrEqualRule<int> rule = new(3);

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
	}

	[Test]
	[Arguments(0.0, true)]
	[Arguments(2.0, true)]
	[Arguments(-4.0, true)]
	[Arguments(3.0, false)]
	[Arguments(-3.0, false)]
	public async Task EvenRule_GivenDouble_ChecksParity(double value, bool expected)
	{
		// Arrange
		EvenRule<double> rule = new();

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
		await Assert.That(rule.Code).IsEqualTo(EvenRule<double>.ErrorCode);
	}

	[Test]
	[Arguments(0, true)]
	[Arguments(2, true)]
	[Arguments(7, false)]
	public async Task EvenRule_GivenInteger_ChecksParity(int value, bool expected)
	{
		// Arrange - the rule is generic over any INumber<T>, so it closes with int as well as double.
		EvenRule<int> rule = new();

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1.0, true)]
	[Arguments(3.0, true)]
	[Arguments(-5.0, true)]
	[Arguments(4.0, false)]
	[Arguments(0.0, false)]
	public async Task OddRule_GivenDouble_ChecksParity(double value, bool expected)
	{
		// Arrange
		OddRule<double> rule = new();

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
		await Assert.That(rule.Code).IsEqualTo(OddRule<double>.ErrorCode);
	}

	[Test]
	[Arguments(1, true)]
	[Arguments(3, true)]
	[Arguments(2, false)]
	public async Task OddRule_GivenInteger_ChecksParity(int value, bool expected)
	{
		// Arrange
		OddRule<int> rule = new();

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
	}

	[Test]
	[Arguments(5)]
	[Arguments(0)]
	[Arguments(-12)]
	public async Task IntRule_GivenInteger_AlwaysHolds(int value)
	{
		// Arrange - the rule is generic over any INumber<T>, so an int member closes it directly.
		IntRule<int> rule = new();

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsTrue();
	}

	[Test]
	[Arguments(25.0, true)]
	[Arguments(25.5, false)]
	[Arguments(double.PositiveInfinity, false)]
	public async Task IntRule_GivenDouble_ChecksWholeness(double value, bool expected)
	{
		// Arrange
		IntRule<double> rule = new();

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
	}

	[Test]
	[Arguments(5)]
	[Arguments(-1)]
	public async Task FiniteRule_GivenInteger_AlwaysHolds(int value)
	{
		// Arrange
		FiniteRule<int> rule = new();

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsTrue();
	}

	[Test]
	[Arguments(1.0, true)]
	[Arguments(double.PositiveInfinity, false)]
	[Arguments(double.NegativeInfinity, false)]
	public async Task FiniteRule_GivenDouble_ChecksFiniteness(double value, bool expected)
	{
		// Arrange
		FiniteRule<double> rule = new();

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
	}

	[Test]
	[Arguments(30, true)]
	[Arguments(25, false)]
	[Arguments(0, true)]
	public async Task MultipleOfRule_GivenInteger_ChecksExactMultiples(int value, bool expected)
	{
		// Arrange - integer types cannot represent the relative tolerance, so the comparison is exact.
		MultipleOfRule<int> rule = new(10);

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
	}

	[Test]
	[Arguments(0.3, true)]
	[Arguments(0.3000000001, false)]
	[Arguments(-0.3, true)]
	public async Task MultipleOfRule_GivenDouble_ToleratesFloatingPointRounding(double value, bool expected)
	{
		// Arrange
		MultipleOfRule<double> rule = new(0.1);

		// Act
		var valid = rule.IsValid(value);

		// Assert
		await Assert.That(valid).IsEqualTo(expected);
	}

	[Test]
	public async Task MultipleOfRule_GivenZeroDivisor_Throws()
	{
		// Act
		var exception = Assert.Throws<ArgumentException>(static () => _ = new MultipleOfRule<int>(0));

		// Assert
		await Assert.That(exception).IsNotNull();
	}

	[Test]
	public async Task EvenRule_GivenMessage_OverridesTheFormattedMessage()
	{
		// Arrange
		EvenRule<double> rule = new("Number must be even.");

		// Act
		var message = rule.GetErrorMessage(3.0);

		// Assert
		await Assert.That(message).IsEqualTo("Number must be even.");
	}

	[Test]
	public async Task Rule_GivenCodeOverride_ReportsItThroughIZodRule()
	{
		// Arrange
		MinLengthRule rule = new(3, code: "too_short");

		// Act
		var code = ((IZodRule)rule).Code;

		// Assert - the canonical constant is unchanged, the per-usage override is reported at runtime.
		await Assert.That(rule.Code).IsEqualTo(MinLengthRule.ErrorCode);
		await Assert.That(code).IsEqualTo("too_short");
	}

	[Test]
	public async Task ZodNumber_GivenEvenAndOdd_ReportsTheRuleCodes()
	{
		// Act
		var even = Z.Number().Even().Validate(3.0);
		var odd = Z.Number().Odd().Validate(4.0);

		// Assert
		await Assert.That(even.Errors[0].Code).IsEqualTo(EvenRule<double>.ErrorCode);
		await Assert.That(odd.Errors[0].Code).IsEqualTo(OddRule<double>.ErrorCode);
	}

	[Test]
	public async Task ZodNumber_GivenGteAndLte_ReportsTheRuleCodes()
	{
		// Act
		var gte = Z.Number().Gte(3.0).Validate(2.0);
		var lte = Z.Number().Lte(3.0).Validate(4.0);

		// Assert
		await Assert.That(gte.Errors[0].Code).IsEqualTo(GreaterThanOrEqualRule<double>.ErrorCode);
		await Assert.That(lte.Errors[0].Code).IsEqualTo(LessThanOrEqualRule<double>.ErrorCode);
	}
}
