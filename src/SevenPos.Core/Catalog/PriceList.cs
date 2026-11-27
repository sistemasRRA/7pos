namespace SevenPos.Core.Catalog;

/// <summary>
/// Doc: Matriz de precios. The prices of one zone or store type ("Lista Norte").
/// Each branch sells from exactly one list. Only Marketing manages lists (ADR 0011).
/// </summary>
public class PriceList : SyncEntity
{
    public required string Name { get; set; }
    public List<PriceListItem> Items { get; set; } = [];
}
