using System.Globalization;
using SevenPos.Core.Catalog;
using SevenPos.Core.Common;

namespace SevenPos.UI;

/// <summary>How values are shown to the user: Mexican Spanish formats and local time.</summary>
public static class Display
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-MX");

    /// <summary>A UTC timestamp from the database, as a local date: 26/11/2026.</summary>
    public static string Date(DateTime utc) => BusinessTime.ToLocal(utc).ToString("dd/MM/yyyy", Culture);

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", Culture);

    /// <summary>A UTC timestamp as local date and time: 26/11/2026 09:00.</summary>
    public static string DateTime(DateTime utc) =>
        BusinessTime.ToLocal(utc).ToString("dd/MM/yyyy HH:mm", Culture);

    /// <summary>Money in Mexican pesos: $1,234.50.</summary>
    public static string Money(decimal amount) => amount.ToString("C2", Culture);

    /// <summary>A modifier option with its group: "Tamaño: Mediano". Just "Urgente" when both names match.</summary>
    public static string Option(Modifier option) =>
        option.Group is null || option.Group.Name == option.Name ? option.Name : $"{option.Group.Name}: {option.Name}";

    /// <summary>What an option adds: "+15%" or "+$159.00".</summary>
    public static string Charge(Modifier option) => option.ChargeType == ChargeType.Fixed
        ? "+" + Money(option.ChargeValue)
        : "+" + option.ChargeValue.ToString("0.##", Culture) + "%";

    /// <summary>Today in Mexico City.</summary>
    public static DateOnly LocalToday(TimeProvider clock) => BusinessTime.Today(clock);

    /// <summary>The UTC moment a local calendar day starts (00:00 in Mexico City).</summary>
    public static DateTime StartOfLocalDayUtc(DateOnly day) => BusinessTime.StartOfDayUtc(day);
}
