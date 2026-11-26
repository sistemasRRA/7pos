using System.Globalization;

namespace SevenPos.UI;

/// <summary>How values are shown to the user: Mexican Spanish formats and local time.</summary>
public static class Display
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-MX");
    private static readonly TimeZoneInfo LocalZone = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    /// <summary>A UTC timestamp from the database, as a local date: 26/11/2026.</summary>
    public static string Date(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), LocalZone).ToString("dd/MM/yyyy", Culture);

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", Culture);
}
