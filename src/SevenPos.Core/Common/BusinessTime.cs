namespace SevenPos.Core.Common;

/// <summary>
/// The business runs on Mexico City time. The database stores UTC (docs/data-rules.md);
/// anything that depends on the calendar day (garment label dates, "from tomorrow"
/// prices, daily reports) converts with these helpers.
/// </summary>
public static class BusinessTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>The local calendar day of a UTC moment: 03:00 UTC on the 27th is still the 26th in Mexico City.</summary>
    public static DateOnly LocalDate(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));

    public static DateOnly Today(TimeProvider clock) => LocalDate(clock.GetUtcNow().UtcDateTime);

    /// <summary>The UTC moment a local calendar day starts (00:00 in Mexico City).</summary>
    public static DateTime StartOfDayUtc(DateOnly day) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), Zone);
}
