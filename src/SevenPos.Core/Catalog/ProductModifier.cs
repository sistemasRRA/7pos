namespace SevenPos.Core.Catalog;

/// <summary>Many-to-many: which modifiers apply to which product.</summary>
public class ProductModifier : SyncEntity
{
    public Guid ProductId { get; set; }
    public Guid ModifierId { get; set; }
    public Modifier? Modifier { get; set; }
}
