namespace SevenPos.Core.Organization;

/// <summary>Doc: Sucursal.</summary>
public class Branch : SyncEntity
{
    public required string Name { get; set; }
    /// <summary>Up to 5 comma-separated emails for shift/alert reports.</summary>
    public string? AlertEmails { get; set; }

    /// <summary>
    /// Doc: Matriz_Precios_ID. The price list this branch sells from. A branch without
    /// a list sells nothing: products without a price are never offered (ADR 0011).
    /// </summary>
    public Guid? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
}
