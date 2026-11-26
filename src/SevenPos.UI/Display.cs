using System.Globalization;

namespace SevenPos.UI;

/// <summary>How values are shown to the user: Mexican Spanish formats and local time.</summary>
public static class Display
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-MX");
    private static readonly TimeZoneInfo LocalZone = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    /// <summary>A UTC timestamp from the database, as a local date: 26/11/2026.</summary>
    public static string Date(DateTime utc) => ToLocal(utc).ToString("dd/MM/yyyy", Culture);

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", Culture);

    /// <summary>A UTC timestamp as local date and time: 26/11/2026 09:00.</summary>
    public static string DateTime(DateTime utc) =>
        ToLocal(utc).ToString("dd/MM/yyyy HH:mm", Culture);

    /// <summary>Money in Mexican pesos: $1,234.50.</summary>
    public static string Money(decimal amount) => amount.ToString("C2", Culture);

    /// <summary>Today in Mexico City.</summary>
    public static DateOnly LocalToday(TimeProvider clock) => DateOnly.FromDateTime(ToLocal(clock.GetUtcNow().UtcDateTime));

    /// <summary>The UTC moment a local calendar day starts (00:00 in Mexico City).</summary>
    public static DateTime StartOfLocalDayUtc(DateOnly day) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), LocalZone);

    private static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.SpecifyKind(utc, DateTimeKind.Utc), LocalZone);
}
