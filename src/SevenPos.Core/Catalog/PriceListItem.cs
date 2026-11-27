namespace SevenPos.Core.Catalog;

/// <summary>
/// Doc: Matriz_Precios_Detalle. A product's price in a list from a given moment on.
/// A price change adds a new row; rows are never updated, so the full price history
/// is kept and prices can be scheduled ahead. The current price is the row with the
/// latest EffectiveFromUtc that is not in the future. Prices include IVA (ADR 0010).
/// </summary>
public class PriceListItem : SyncEntity
{
    public Guid PriceListId { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Price { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    /// <summary>The Marketing user who set this price.</summary>
    public Guid SetByUserId { get; set; }
}
