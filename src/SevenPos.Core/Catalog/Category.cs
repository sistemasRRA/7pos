namespace SevenPos.Core.Catalog;

/// <summary>Doc: Familia (e.g. Camisas, Blancos, Tenis).</summary>
public class Category : SyncEntity
{
    public required string Name { get; set; }
    public List<Product> Products { get; set; } = [];
}
