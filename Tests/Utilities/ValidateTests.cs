using equiavia.components.Utilities;
using Xunit;

namespace equiavia.components.Tests.Utilities;

public class ValidateTests
{
    // ── IsTheSame ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsTheSame_DoesNotThrow_WhenStringsAreEqual()
    {
        var ex = Record.Exception(() => Validate.IsTheSame("abc", "abc"));
        Assert.Null(ex);
    }

    [Fact]
    public void IsTheSame_DoesNotThrow_WhenIntsAreEqual()
    {
        var ex = Record.Exception(() => Validate.IsTheSame(42, 42));
        Assert.Null(ex);
    }

    [Fact]
    public void IsTheSame_Throws_WhenValuesAreNotEqual()
    {
        Assert.Throws<ValidationException>(() => Validate.IsTheSame("abc", "xyz"));
    }

    // ── IsNull ────────────────────────────────────────────────────────────────

    [Fact]
    public void IsNull_DoesNotThrow_WhenValueIsNull()
    {
        var ex = Record.Exception(() => Validate.IsNull(null));
        Assert.Null(ex);
    }

    [Fact]
    public void IsNull_Throws_WhenValueIsNotNull()
    {
        Assert.Throws<ValidationException>(() => Validate.IsNull("not null"));
    }

    // ── IsNotNull ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsNotNull_DoesNotThrow_WhenValueIsNotNull()
    {
        var ex = Record.Exception(() => Validate.IsNotNull("value"));
        Assert.Null(ex);
    }

    [Fact]
    public void IsNotNull_Throws_WhenValueIsNull()
    {
        Assert.Throws<ValidationException>(() => Validate.IsNotNull(null));
    }

    // ── StringHasValue ────────────────────────────────────────────────────────

    [Fact]
    public void StringHasValue_DoesNotThrow_WhenStringHasValue()
    {
        var ex = Record.Exception(() => Validate.StringHasValue("hello"));
        Assert.Null(ex);
    }

    [Fact]
    public void StringHasValue_Throws_WhenStringIsNull()
    {
        Assert.Throws<ValidationException>(() => Validate.StringHasValue(null!));
    }

    [Fact]
    public void StringHasValue_Throws_WhenStringIsEmpty()
    {
        Assert.Throws<ValidationException>(() => Validate.StringHasValue(""));
    }

    // ── ValidationException ───────────────────────────────────────────────────

    [Fact]
    public void ValidationException_InheritsFromException()
    {
        var ex = new ValidationException("test message");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("test message", ex.Message);
    }
}
