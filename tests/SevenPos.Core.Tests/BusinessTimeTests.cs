using SevenPos.Core.Common;

namespace SevenPos.Core.Tests;

public class BusinessTimeTests
{
    [Fact]
    public void LocalDate_LateEveningInMexicoCity_IsStillThatDay()
    {
        // 03:00 UTC on the 27th is 21:00 on the 26th in Mexico City (UTC-6).
        var utc = new DateTime(2026, 11, 27, 3, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2026, 11, 26), BusinessTime.LocalDate(utc));
    }

    [Fact]
    public void StartOfDayUtc_IsLocalMidnight()
    {
        Assert.Equal(
            new DateTime(2026, 11, 26, 6, 0, 0, DateTimeKind.Utc),
            BusinessTime.StartOfDayUtc(new DateOnly(2026, 11, 26)));
    }
}
