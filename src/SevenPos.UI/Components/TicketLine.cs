using SevenPos.Core.Catalog;

namespace SevenPos.UI.Components;

/// <summary>
/// Garments of the same kind with the same extras, captured together in the wizard
/// (e.g. 3 shirts with starch). Saved as one Garment row per piece.
/// The price here is a preview: TicketService prices the ticket again when it is saved.
/// </summary>
public sealed record TicketLine(CatalogEntry Entry, IReadOnlyList<Modifier> Modifiers, int Quantity)
{
    public PriceResult UnitPrice => PriceCalculator.Calculate(Entry.Price, Modifiers);
    public decimal Total => UnitPrice.Total * Quantity;
}
