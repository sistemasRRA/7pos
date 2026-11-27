namespace SevenPos.Core.Catalog;

public class Product : SyncEntity
{
    public Guid ServiceLineId { get; set; }
    public ServiceLine? ServiceLine { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public required string Name { get; set; }
    public decimal BasePrice { get; set; }
    /// <summary>The modifier options this product offers. Groups without any option here are not asked.</summary>
    public List<ProductModifier> Modifiers { get; set; } = [];
}
