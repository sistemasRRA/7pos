using SevenPos.Core.Guests;

namespace SevenPos.Core.Tests;

/// <summary>Unit tests: pure logic, no database.</summary>
public class PhoneNumberTests
{
    [Theory]
    [InlineData("5512345678", "5512345678")]
    [InlineData("55 1234 5678", "5512345678")]
    [InlineData("(55) 1234-5678", "5512345678")]
    [InlineData("55.1234.5678", "5512345678")]
    [InlineData("+52 55 1234 5678", "5512345678")]
    [InlineData("525512345678", "5512345678")]
    [InlineData("+52 1 55 1234 5678", "5512345678")] // old mobile format with the extra 1
    public void ValidMexicanNumbers_AreNormalizedToTenDigits(string input, string expected)
    {
        Assert.True(PhoneNumber.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("551234567")]       // 9 digits
    [InlineData("55123456789")]     // 11 digits
    [InlineData("+1 555 123 4567")] // not Mexican
    [InlineData("55-ABCD-5678")]
    public void InvalidNumbers_AreRejected(string? input)
    {
        Assert.False(PhoneNumber.TryNormalize(input, out _));
    }

    [Fact]
    public void Format_GroupsDigitsForDisplay()
    {
        Assert.Equal("55 1234 5678", PhoneNumber.Format("5512345678"));
    }

    [Fact]
    public void Format_LeavesUnexpectedValuesUntouched()
    {
        Assert.Equal("123", PhoneNumber.Format("123"));
    }
}
