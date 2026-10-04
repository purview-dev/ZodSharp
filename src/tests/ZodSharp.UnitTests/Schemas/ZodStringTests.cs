using ZodSharp.Core;
using ZodSharp.Rules;

namespace ZodSharp.Schemas;

public class ZodStringTests
{
	[Test]
	[Arguments("John", true)]
	[Arguments("AB", false)]
	[Arguments("ABC", true)]
	public async Task StringMin_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Min(3).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	public async Task StringMin_GivenEmptyStringAndZeroMinimum_ReturnsSuccess()
	{
		var result = Z.String().Min(0).Validate(string.Empty);

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task StringMax_GivenTooLongValue_ReturnsFailure()
	{
		var result = Z.String().Max(50).Validate(new string('a', 51));

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors).Count().IsEqualTo(1);
	}

	[Test]
	public async Task StringMax_GivenExactlyMaximumLength_ReturnsSuccess()
	{
		var result = Z.String().Max(50).Validate(new string('a', 50));

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task StringMax_GivenVeryLongValue_ReturnsFailure()
	{
		var result = Z.String().Max(50).Validate(new string('a', 1001));

		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	[Arguments("user@example.com", true)]
	[Arguments("first.last+tag@domain.co.uk", true)]
	[Arguments("invalid", false)]
	[Arguments("user@", false)]
	[Arguments("@example.com", false)]
	public async Task StringEmail_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Email().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("https://example.com", true)]
	[Arguments("http://sub.domain.co.uk/path?x=1#fragment", true)]
	[Arguments("not-a-url", false)]
	public async Task StringUrl_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Url().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("550e8400-e29b-41d4-a716-446655440000", true)]
	[Arguments("550e8400-e29b-11d4-a716-446655440000", true)]
	[Arguments("0192b4c1-7a9b-7f5e-9a3c-2d4e6f8a0b1c", true)]
	[Arguments("550E8400-E29B-41D4-A716-446655440000", true)]
	[Arguments("00000000-0000-0000-0000-000000000000", true)]
	[Arguments("ffffffff-ffff-ffff-ffff-ffffffffffff", true)]
	[Arguments("550e8400-e29b-01d4-a716-446655440000", false)]
	[Arguments("550e8400-e29b-91d4-a716-446655440000", false)]
	[Arguments("550e8400-e29b-f1d4-a716-446655440000", false)]
	[Arguments("550e8400-e29b-41d4-0716-446655440000", false)]
	[Arguments("550e8400-e29b-41d4-5716-446655440000", false)]
	[Arguments("550e8400-e29b-41d4-c716-446655440000", false)]
	[Arguments("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF", false)]
	[Arguments("not-a-uuid", false)]
	[Arguments("550e8400-e29b-41d4-a716", false)]
	public async Task StringUuid_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().UUID().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(UuidVersion.V7, "0192b4c1-7a9b-7f5e-9a3c-2d4e6f8a0b1c", true)]
	[Arguments(UuidVersion.V7, "550e8400-e29b-41d4-a716-446655440000", false)]
	[Arguments(UuidVersion.V7, "0192b4c1-7a9b-7f5e-03a3-2d4e6f8a0b1c", false)]
	[Arguments(UuidVersion.V4, "550e8400-e29b-41d4-a716-446655440000", true)]
	[Arguments(UuidVersion.V4, "00000000-0000-0000-0000-000000000000", false)]
	[Arguments(UuidVersion.V4, "ffffffff-ffff-ffff-ffff-ffffffffffff", false)]
	[Arguments(UuidVersion.V8, "550e8400-e29b-81d4-a716-446655440000", true)]
	public async Task StringUuid_GivenVersion_ReturnsExpectedResult(UuidVersion version, string value, bool expected)
	{
		var result = Z.String().UUID(version).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	public async Task StringStartsWith_GivenMatchingPrefix_ReturnsSuccess()
	{
		var result = Z.String().StartsWith("https://").Validate("https://example.com");

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task StringStartsWith_GivenNonMatchingPrefix_ReturnsFailure()
	{
		var result = Z.String().StartsWith("https://").Validate("http://example.com");

		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task StringEndsWith_GivenMatchingSuffix_ReturnsSuccess()
	{
		var result = Z.String().EndsWith(".com").Validate("example.com");

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task StringEndsWith_GivenNonMatchingSuffix_ReturnsFailure()
	{
		var result = Z.String().EndsWith(".com").Validate("example.org");

		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task StringStartsWith_GivenOrdinalIgnoreCaseComparison_IgnoresCase()
	{
		var result = Z.String()
			.StartsWith("HTTPS://", StringComparison.OrdinalIgnoreCase)
			.Validate("https://example.com");

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task StringEndsWith_GivenOrdinalIgnoreCaseComparison_IgnoresCase()
	{
		var result = Z.String().EndsWith(".COM", StringComparison.OrdinalIgnoreCase).Validate("example.com");

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task StringStartsWith_GivenCustomMessageAndCode_ReportsThem()
	{
		var result = Z.String()
			.StartsWith("https://", message: "Must start with https.", code: "bad_prefix")
			.Validate("http://example.com");

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo("bad_prefix");
		await Assert.That(result.Errors[0].Message).IsEqualTo("Must start with https.");
	}

	[Test]
	[Arguments("  hello  ", "hello")]
	[Arguments("\t hello \t", "hello")]
	[Arguments("hello", "hello")]
	public async Task StringTrim_GivenValue_ReturnsTrimmedValue(string value, string expected)
	{
		var result = Z.String().Trim().Validate(value);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo(expected);
	}

	[Test]
	public async Task StringToUpper_GivenLowercase_ReturnsUppercaseValue()
	{
		var result = Z.String().ToUpper().Validate("hello");

		await Assert.That(result.Value).IsEqualTo("HELLO");
	}

	[Test]
	public async Task StringToLower_GivenUppercase_ReturnsLowercaseValue()
	{
		var result = Z.String().ToLower().Validate("HELLO");

		await Assert.That(result.Value).IsEqualTo("hello");
	}

	[Test]
	[Arguments("1234567890", true)]
	[Arguments("123", false)]
	[Arguments("12345678901", false)]
	public async Task StringLength_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Length(10).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	public async Task ValidateSpan_GivenValidEmailSpan_ReturnsSuccess()
	{
		var result = Z.String().Min(3).Max(50).Email().ValidateSpan("user@example.com".AsSpan());

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task ValidateSpan_GivenInvalidEmailSpan_ReturnsFailure()
	{
		var result = Z.String().Email().ValidateSpan("not-an-email".AsSpan());

		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task Parse_GivenInvalidString_ThrowsZodExceptionWithErrors()
	{
		var exception = Assert.Throws<ZodException>(static () => Z.String().Min(3).Parse("AB"));

		await Assert.That(exception).IsNotNull();
		await Assert.That(exception.Errors).Count().IsEqualTo(1);
	}

	[Test]
	[Arguments("hello world", "world", true)]
	[Arguments("hello world", "planet", false)]
	[Arguments("", "x", false)]
	public async Task StringIncludes_GivenValue_ReturnsExpectedResult(string value, string substring, bool expected)
	{
		var result = Z.String().Includes(substring).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("192.168.1.1", true)]
	[Arguments("255.255.255.255", true)]
	[Arguments("::1", true)]
	[Arguments("2001:db8::8a2e:370:7334", true)]
	[Arguments("999.1.1.1", false)]
	[Arguments("not-an-ip", false)]
	public async Task StringIP_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().IP().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(IPAddressRuleType.IPv4, "192.168.1.1", true)]
	[Arguments(IPAddressRuleType.IPv4, "::1", false)]
	[Arguments(IPAddressRuleType.IPv6, "::1", true)]
	[Arguments(IPAddressRuleType.IPv6, "192.168.1.1", false)]
	[Arguments(IPAddressRuleType.Any, "2001:db8::8a2e:370:7334", true)]
	public async Task StringIP_GivenRuleType_ReturnsExpectedResult(
		IPAddressRuleType ruleType,
		string value,
		bool expected
	)
	{
		var result = Z.String().IP(ruleType).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U", true)]
	[Arguments("a.b.c", true)]
	[Arguments("abc", false)]
	[Arguments("a..c", false)]
	[Arguments(".b.c", false)]
	public async Task StringJwt_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().JWT().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("deadBEEF", true)]
	[Arguments("0x1A", false)]
	[Arguments("", true)]
	[Arguments("zz", false)]
	public async Task StringHex_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Hex().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("SGVsbG8", true)]
	[Arguments("a_b-c9", true)]
	[Arguments("", true)]
	[Arguments("SGVsbG8=", false)]
	[Arguments("SGVsbG8==", false)]
	[Arguments("a", false)]
	[Arguments("not+a+base64", false)]
	public async Task StringBase64Url_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Base64Url().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("01ARZ3NDEKTSV4RRFFQ69G5FAV", true)]
	[Arguments("01arz3ndektsv4rrffq69g5fav", true)]
	[Arguments("01ARZ3NDEKTSV4RRFFQ69G5FA", false)]
	[Arguments("01ARZ3NDEKTSV4RRFFQ69G5FAVZ", false)]
	[Arguments("01ARZ3NDEKTSV4RRFFQ69G5FAU", false)]
	[Arguments("81ARZ3NDEKTSV4RRFFQ69G5FAV", false)]
	public async Task StringUlid_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().ULID().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("1970-01-01T00:00:00.000Z", true)]
	[Arguments("2022-10-13T09:52:31.8162314Z", true)]
	[Arguments("1970-01-01T00:00:00Z", true)]
	[Arguments("2022-10-13T09:52:31Z", true)]
	[Arguments("2020-10-14", false)]
	[Arguments("2020-10-14T17:42:29+00:00", false)]
	[Arguments("2020-13-01T00:00:00Z", false)]
	[Arguments("not-a-datetime", false)]
	public async Task StringDatetime_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Datetime().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("2020-01-01", true)]
	[Arguments("2020-12-31", true)]
	[Arguments("2020-1-1", false)]
	[Arguments("2020-13-01", false)]
	[Arguments("2000-02-30", false)]
	[Arguments("2020-01-01T00:00:00Z", false)]
	public async Task StringDate_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Date().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("00:00:00", true)]
	[Arguments("23:59:59", true)]
	[Arguments("23:59:59.9999999", true)]
	[Arguments("00:00", true)]
	[Arguments("24:00:00", false)]
	[Arguments("00:60:00", false)]
	[Arguments("00:00:60", false)]
	[Arguments("0:00:00", false)]
	public async Task StringTime_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Time().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("V1StGXR8_Z5jdHi6B-myT", true)]
	[Arguments("V1StGXR8_Z5jdHi6B-my", false)]
	[Arguments("V1StGXR8_Z5jdHi6B-myTT", false)]
	[Arguments("V1StGXR8_Z5jdHi6B+myt", false)]
	public async Task StringNanoid_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Nanoid().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("tz4a98xxat96iws9zmbrgj3a", true)]
	[Arguments("a", true)]
	[Arguments("", false)]
	[Arguments("TZ4A98XXAT96IWS9ZMBRGJ3A", false)]
	public async Task StringCuid2_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Cuid2().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("+14155552671", true)]
	[Arguments("+1234567", true)]
	[Arguments("123456789", false)]
	[Arguments("+01234567", false)]
	[Arguments("+123456", false)]
	public async Task StringE164_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().E164().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments("https://example.com/path?x=1", true)]
	[Arguments("/relative/path", true)]
	[Arguments("relative/path", true)]
	[Arguments("", false)]
	[Arguments("   ", false)]
	[Arguments("http://[invalid", false)]
	public async Task StringUri_GivenValue_ReturnsExpectedResult(string value, bool expected)
	{
		var result = Z.String().Uri().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(UriKind.Absolute, "https://example.com", true)]
	[Arguments(UriKind.Absolute, "relative/path", false)]
	[Arguments(UriKind.Relative, "relative/path", true)]
	public async Task StringUri_GivenUriKind_ReturnsExpectedResult(UriKind uriKind, string value, bool expected)
	{
		var result = Z.String().Uri(uriKind).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	public async Task StringUri_GivenCustomMessage_ReportsMessage()
	{
		var result = Z.String().Uri(UriKind.Absolute, "Must be an absolute URI.").Validate("relative/path");

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors[0].Code).IsEqualTo(UriRule.ErrorCode);
		await Assert.That(result.Errors[0].Message).IsEqualTo("Must be an absolute URI.");
	}
}
