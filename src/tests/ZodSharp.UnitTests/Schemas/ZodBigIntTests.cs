namespace ZodSharp.Schemas;

public class ZodBigIntTests
{
	[Test]
	[Arguments(10L, 5L, true)]
	[Arguments(5L, 5L, false)]
	[Arguments(4L, 5L, false)]
	public async Task BigIntGt_GivenValue_ReturnsExpectedResult(long value, long bound, bool expected)
	{
		var result = Z.BigInt().Gt(bound).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(5L, 5L, true)]
	[Arguments(6L, 5L, true)]
	[Arguments(4L, 5L, false)]
	public async Task BigIntGte_GivenValue_ReturnsExpectedResult(long value, long bound, bool expected)
	{
		var result = Z.BigInt().Gte(bound).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(5L, 5L, false)]
	[Arguments(6L, 5L, false)]
	[Arguments(4L, 5L, true)]
	public async Task BigIntLt_GivenValue_ReturnsExpectedResult(long value, long bound, bool expected)
	{
		var result = Z.BigInt().Lt(bound).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(5L, 5L, true)]
	[Arguments(6L, 5L, false)]
	[Arguments(4L, 5L, true)]
	public async Task BigIntLte_GivenValue_ReturnsExpectedResult(long value, long bound, bool expected)
	{
		var result = Z.BigInt().Lte(bound).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(10L, 0L, true)]
	[Arguments(0L, 0L, true)]
	[Arguments(-10L, 0L, false)]
	public async Task BigIntMin_GivenValue_ReturnsExpectedResult(long value, long minValue, bool expected)
	{
		var result = Z.BigInt().Min(minValue).Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1L, true)]
	[Arguments(0L, false)]
	[Arguments(-1L, false)]
	public async Task BigIntPositive_GivenValue_ReturnsExpectedResult(long value, bool expected)
	{
		var result = Z.BigInt().Positive().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(-1L, true)]
	[Arguments(0L, false)]
	[Arguments(1L, false)]
	public async Task BigIntNegative_GivenValue_ReturnsExpectedResult(long value, bool expected)
	{
		var result = Z.BigInt().Negative().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}

	[Test]
	[Arguments(0L, true)]
	[Arguments(1L, true)]
	[Arguments(-1L, false)]
	public async Task BigIntNonNegative_GivenValue_ReturnsExpectedResult(long value, bool expected)
	{
		var result = Z.BigInt().NonNegative().Validate(value);

		await Assert.That(result.IsSuccess).IsEqualTo(expected);
	}
}
