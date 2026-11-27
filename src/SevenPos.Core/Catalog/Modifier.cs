namespace SevenPos.Core.Catalog;

public enum ChargeType { Fixed, Percentage }

/// <summary>Doc: Modificador (upsell extras, e.g. Almidón, Express, Suela blanca).</summary>
public class Modifier : SyncEntity
{
    public required string Name { get; set; }
    public ChargeType ChargeType { get; set; }
    /// <summary>Pesos if Fixed; 50 = 50% of the list price if Percentage (ADR 0012).</summary>
    public decimal ChargeValue { get; set; }
    /// <summary>
    /// Order in which the wizard asks for it and lists it on the ticket.
    /// It does not change the price: percentages are always of the list price.
    /// </summary>
    public int SortOrder { get; set; }
}
