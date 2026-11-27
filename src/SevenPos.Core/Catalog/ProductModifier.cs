namespace SevenPos.Core.Catalog;

/// <summary>Many-to-many: which modifier options a product offers.</summary>
public class ProductModifier : SyncEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid ModifierId { get; set; }
    public Modifier? Modifier { get; set; }
}
