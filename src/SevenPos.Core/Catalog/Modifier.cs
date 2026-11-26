namespace SevenPos.Core.Catalog;

public enum ChargeType { Fixed, Percentage }

/// <summary>Doc: Modificador (upsell extras, e.g. Almidón, Express, Suela blanca).</summary>
public class Modifier : SyncEntity
{
    public required string Name { get; set; }
    public ChargeType ChargeType { get; set; }
    /// <summary>Pesos if Fixed; 50 = 50% if Percentage.</summary>
    public decimal ChargeValue { get; set; }
    /// <summary>
    /// Order in which the wizard asks for it and applies it. It matters:
    /// a percentage is calculated on the running subtotal at that step.
    /// </summary>
    public int SortOrder { get; set; }
}
