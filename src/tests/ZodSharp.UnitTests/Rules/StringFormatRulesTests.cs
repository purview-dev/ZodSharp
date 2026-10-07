namespace ZodSharp.Rules;

public class StringFormatRulesTests
{
	[Test]
	[Arguments("9m4e2mr0ui3e8a215n4g", true)]
	[Arguments("9M4E2MR0UI3E8A215N4G", true)]
	[Arguments("9m4e2mr0ui3e8a215n4", false)]
	[Arguments("9m4e2mr0ui3e8a215n4z", false)]
	[Arguments("", false)]
	public async Task Xid_GivenValue_ValidatesTheTwentyCharacterBase32HexFormat(
		string value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		XidRule rule = new();

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	[Arguments("0o5Fs0EELR0fUjHjbCnEtdUwQe3", true)]
	[Arguments("0o5Fs0EELR0fUjHjbCnEtdUwQe", false)]
	[Arguments("0o5Fs0EELR0fUjHjbCnEtdUwQe-", false)]
	public async Task Ksuid_GivenValue_ValidatesTheTwentySevenCharacterBase62Format(
		string value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		KsuidRule rule = new();

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	[Arguments("550e8400-e29b-41d4-a716-446655440000", true)]
	[Arguments("00000000-0000-0000-0000-000000000000", true)]
	[Arguments("550e8400-e29b-41d4-a716-44665544000g", false)]
	[Arguments("550e8400e29b41d4a716446655440000", false)]
	public async Task Guid_GivenValue_ValidatesTheEightFourFourFourTwelveFormat(
		string value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		GuidRule rule = new();

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	[Arguments("P3Y6M4DT12H30M5S", true)]
	[Arguments("P1W", true)]
	[Arguments("PT1H", true)]
	[Arguments("P1Y2M3DT4H5M6.7S", true)]
	[Arguments("P", false)]
	[Arguments("3Y", false)]
	[Arguments("P1Y2W", false)]
	public async Task Duration_GivenValue_ValidatesIso8601Durations(
		string value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		DurationRule rule = new();

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	[Arguments("192.168.0.0/24", true)]
	[Arguments("10.0.0.0/8", true)]
	[Arguments("192.168.0.0/33", false)]
	[Arguments("192.168.0.0", false)]
	[Arguments("not-an-address/24", false)]
	public async Task Cidr_GivenIPv4Value_ValidatesTheBlock(
		string value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		CidrRule rule = new(CidrRuleType.IPv4);

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	public async Task Cidr_GivenIPv6Value_RejectsItForTheIPv4Rule(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		CidrRule rule = new(CidrRuleType.IPv4);

		// Act
		var isValid = rule.IsValid("2001:db8::/32");

		// Assert
		await Assert.That(isValid).IsFalse();
	}

	[Test]
	public async Task Cidr_GivenAnyRule_AcceptsBothFamilies(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		CidrRule rule = new();

		// Act
		var ipv4 = rule.IsValid("192.168.0.0/24");
		var ipv6 = rule.IsValid("2001:db8::/32");

		// Assert
		await Assert.That(ipv4).IsTrue();
		await Assert.That(ipv6).IsTrue();
	}

	[Test]
	[Arguments("😀", true)]
	[Arguments("👍🏽", true)]
	[Arguments("🇬🇧", true)]
	[Arguments("1️⃣", true)]
	[Arguments("abc", false)]
	[Arguments("", false)]
	[Arguments("hello 😀", false)]
	public async Task Emoji_GivenValue_ValidatesEmojiOnlyStrings(
		string value,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Arrange
		EmojiRule rule = new();

		// Act
		var isValid = rule.IsValid(value);

		// Assert
		await Assert.That(isValid).IsEqualTo(expected);
	}

	[Test]
	public async Task FormatRules_GivenFluentApi_ReportTheRuleErrorCode(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Act
		var xid = Z.String().Xid().Validate("not-an-xid");
		var ksuid = Z.String().Ksuid().Validate("not-a-ksuid");
		var guid = Z.String().Guid().Validate("not-a-guid");
		var duration = Z.String().Duration().Validate("not-a-duration");
		var cidr = Z.String().Cidr().Validate("not-a-cidr");
		var emoji = Z.String().Emoji().Validate("not-emoji");

		// Assert
		await Assert.That(xid.Errors[0].Code).IsEqualTo(XidRule.ErrorCode);
		await Assert.That(ksuid.Errors[0].Code).IsEqualTo(KsuidRule.ErrorCode);
		await Assert.That(guid.Errors[0].Code).IsEqualTo(GuidRule.ErrorCode);
		await Assert.That(duration.Errors[0].Code).IsEqualTo(DurationRule.ErrorCode);
		await Assert.That(cidr.Errors[0].Code).IsEqualTo(CidrRule.ErrorCode);
		await Assert.That(emoji.Errors[0].Code).IsEqualTo(EmojiRule.ErrorCode);
	}
}
