namespace SevenPos.Core.Catalog;

public class Product : SyncEntity
{
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public required string Name { get; set; }
    public decimal BasePrice { get; set; }
    public List<ProductModifier> Modifiers { get; set; } = [];
}
