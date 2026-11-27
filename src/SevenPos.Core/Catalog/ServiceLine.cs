namespace SevenPos.Core.Catalog;

/// <summary>
/// Doc: Proceso / línea de servicio (Tintorería, Planchado, Sastrería, 7shoes, 7PLUS…).
/// Each product belongs to one line, and each line has its own modifier groups. The same
/// garment can be sold by several lines at different prices (CAMISA in Tintorería and in Planchado).
/// </summary>
public class ServiceLine : SyncEntity
{
    public required string Name { get; set; }
    /// <summary>Order of the lines on the screen.</summary>
    public int SortOrder { get; set; }
    public List<ModifierGroup> ModifierGroups { get; set; } = [];
}
