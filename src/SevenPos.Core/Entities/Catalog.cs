namespace SevenPos.Core.Entities;

/// <summary>Doc: Familia (e.g. Camisas, Blancos, Tenis).</summary>
public class Category : SyncEntity
{
    public required string Name { get; set; }
    public List<Product> Products { get; set; } = [];
}

public class Product : SyncEntity
{
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public required string Name { get; set; }
    public decimal BasePrice { get; set; }
    public List<ProductModifier> Modifiers { get; set; } = [];
}

public enum ChargeType { Fixed, Percentage }

/// <summary>Doc: Modificador (upsell extras, e.g. Almidón, Express, Suela blanca).</summary>
public class Modifier : SyncEntity
{
    public required string Name { get; set; }
    public ChargeType ChargeType { get; set; }
    /// <summary>Pesos if Fixed; 50 = 50% if Percentage.</summary>
    public decimal ChargeValue { get; set; }
    /// <summary>
    /// Order in which the wizard asks for it and applies it. It matters:
    /// a percentage is calculated on the running subtotal at that step.
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>Many-to-many: which modifiers apply to which product.</summary>
public class ProductModifier : SyncEntity
{
    public Guid ProductId { get; set; }
    public Guid ModifierId { get; set; }
    public Modifier? Modifier { get; set; }
}
