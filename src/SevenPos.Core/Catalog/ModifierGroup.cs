namespace SevenPos.Core.Catalog;

/// <summary>
/// A question the wizard asks about a garment (Tamaño, Textil, Almidón…), with its options
/// (the Modifiers). The cashier picks at most one option per group. Groups belong to a
/// service line and are asked in SortOrder, the step number on Marketing's price lists (1–8).
/// </summary>
public class ModifierGroup : SyncEntity
{
    public Guid ServiceLineId { get; set; }
    public required string Name { get; set; }
    /// <summary>The step number: the order the wizard asks the groups.</summary>
    public int SortOrder { get; set; }
    public List<Modifier> Modifiers { get; set; } = [];
}
