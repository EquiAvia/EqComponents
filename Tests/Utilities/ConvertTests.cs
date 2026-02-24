using Xunit;
using EqConvert = equiavia.components.Utilities.Convert;

namespace equiavia.components.Tests.Utilities;

public class ConvertTests
{
    // ── IntTryParse ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("42",   0, 42)]
    [InlineData("-10",  0, -10)]
    [InlineData("0",    5, 0)]
    [InlineData("abc",  5, 5)]
    [InlineData("",     7, 7)]
    [InlineData(null,   3, 3)]
    public void IntTryParse_ReturnsExpected(string? input, int defaultValue, int expected)
    {
        Assert.Equal(expected, EqConvert.IntTryParse(input!, defaultValue));
    }

    // ── IntorNullTryParse ─────────────────────────────────────────────────────

    [Fact]
    public void IntorNullTryParse_ReturnsInt_ForValidString()
    {
        Assert.Equal(10, EqConvert.IntorNullTryParse("10"));
    }

    [Fact]
    public void IntorNullTryParse_ReturnsNegativeInt()
    {
        Assert.Equal(-5, EqConvert.IntorNullTryParse("-5"));
    }

    [Fact]
    public void IntorNullTryParse_ReturnsNull_ForInvalidString()
    {
        Assert.Null(EqConvert.IntorNullTryParse("abc"));
    }

    [Fact]
    public void IntorNullTryParse_ReturnsNull_ForNull()
    {
        Assert.Null(EqConvert.IntorNullTryParse(null!));
    }

    [Fact]
    public void IntorNullTryParse_ReturnsNull_ForEmptyString()
    {
        Assert.Null(EqConvert.IntorNullTryParse(""));
    }

    // ── Base64Encode / Base64Decode ───────────────────────────────────────────

    [Fact]
    public void Base64Encode_ProducesExpectedValue()
    {
        Assert.Equal("aGVsbG8=", EqConvert.Base64Encode("hello"));
    }

    [Fact]
    public void Base64Decode_ProducesExpectedValue()
    {
        Assert.Equal("hello", EqConvert.Base64Decode("aGVsbG8="));
    }

    [Fact]
    public void Base64RoundTrip_ReturnsOriginalString()
    {
        var original = "EqComponents Unit Test 123!";
        Assert.Equal(original, EqConvert.Base64Decode(EqConvert.Base64Encode(original)));
    }

    [Fact]
    public void Base64Encode_HandlesEmptyString()
    {
        Assert.Equal("", EqConvert.Base64Encode(""));
    }
}
