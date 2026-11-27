namespace SevenPos.Core.Catalog;

public enum ChargeType { Fixed, Percentage }

/// <summary>
/// Doc: Modificador. One option of a modifier group, e.g. "Mediano" in Tamaño or
/// "Rígido" in Almidón. Which options each product offers is set in ProductModifier.
/// </summary>
public class Modifier : SyncEntity
{
    public Guid ModifierGroupId { get; set; }
    public ModifierGroup? Group { get; set; }
    public required string Name { get; set; }
    public ChargeType ChargeType { get; set; }
    /// <summary>Pesos if Fixed; 50 = 50% of the list price if Percentage (ADR 0012).</summary>
    public decimal ChargeValue { get; set; }
    /// <summary>
    /// Order of the option within its group on the screen. It never changes the price:
    /// percentages are always of the list price (ADR 0012).
    /// </summary>
    public int SortOrder { get; set; }
}
