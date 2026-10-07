namespace ZodSharp.Rules;

public class DataAnnotationRulesTests
{
	[Test]
	[Arguments(1, true)]
	[Arguments(10, true)]
	[Arguments(5, true)]
	[Arguments(0, false)]
	[Arguments(11, false)]
	public async Task Range_GivenInclusiveBounds_ValidatesTheValue(
		int value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		RangeRule<int> rule = new(1, 10);

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, false)]
	[Arguments(10, false)]
	[Arguments(5, true)]
	public async Task Range_GivenExclusiveBounds_ExcludesTheEndpoints(
		int value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		RangeRule<int> rule = new(1, 10, minimumIsExclusive: true, maximumIsExclusive: true);

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	[Arguments("ab", true)]
	[Arguments("abcde", true)]
	[Arguments("a", false)]
	[Arguments("abcdef", false)]
	public async Task Length_GivenInclusiveBounds_ValidatesTheStringLength(
		string value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		LengthRule rule = new(2, 5);

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	[Arguments("ab", true)]
	[Arguments("abcde", true)]
	[Arguments("a", false)]
	[Arguments("abcdef", false)]
	public async Task StringLength_GivenBounds_ValidatesTheStringLength(
		string value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		StringLengthRule rule = new(5, 2);

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	[Arguments("secret", true)]
	[Arguments("other", false)]
	public async Task Compare_GivenValue_ComparesAgainstTheExpectedValue(
		string value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		CompareRule rule = new("secret");

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	[Arguments(2, true)]
	[Arguments(4, false)]
	public async Task AllowedValues_GivenArray_AcceptsOnlyTheAllowedSet(
		int value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		AllowedValuesRule<int> rule = new([1, 2, 3]);

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	public async Task AllowedValues_GivenSingleValue_AcceptsIt(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		AllowedValuesRule<int> rule = new(5);

		// Act
		var isValid = rule.IsValid(5);

		// Assert
		await Assert.That(isValid).IsTrue();
	}

	[Test]
	[Arguments(4, true)]
	[Arguments(2, false)]
	public async Task DeniedValues_GivenArray_RejectsTheDeniedSet(
		int value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		DeniedValuesRule<int> rule = new([1, 2, 3]);

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	public async Task DataAnnotationRules_GivenFluentApi_ValidateThroughTheSchema(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Act
		var range = Z.Number().Range(1, 10).Validate(11);
		var length = Z.String().Length(2, 5).Validate("abcdef");
		var stringLength = Z.String().StringLength(5, 2).Validate("a");
		var compare = Z.String().Compare("secret").Validate("other");
		var allowed = Z.String().AllowedValues(["a", "b"]).Validate("c");
		var denied = Z.String().DeniedValues(["a", "b"]).Validate("a");

		// Assert
		await Assert.That(range.Errors[0].Code).IsEqualTo(RangeRule<int>.ErrorCode);
		await Assert.That(length.Errors[0].Code).IsEqualTo(LengthRule.ErrorCode);
		await Assert.That(stringLength.Errors[0].Code).IsEqualTo(StringLengthRule.ErrorCode);
		await Assert.That(compare.Errors[0].Code).IsEqualTo(CompareRule.ErrorCode);
		await Assert.That(allowed.Errors[0].Code).IsEqualTo(AllowedValuesRule<string>.ErrorCode);
		await Assert.That(denied.Errors[0].Code).IsEqualTo(DeniedValuesRule<string>.ErrorCode);
	}
}
