namespace SevenPos.Core.Tickets;

public enum GarmentStatus { Dirty, InTransit, Washed, Ironed, Assembled, Delivered }

/// <summary>Doc: Ticket_Items_Pulverizado. One row per physical garment.</summary>
public class Garment : SyncEntity
{
    /// <summary>[BRANCH]-[YYYYMMDD]-[FOLIO]-[SEQ], printed on the label and scanned.</summary>
    public required string Code { get; set; }
    public Guid TicketId { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    /// <summary>Doc: Precio_Lista. The branch's list price at the moment of sale, frozen.</summary>
    public decimal ListPrice { get; set; }
    /// <summary>List price + modifiers at the moment of sale, frozen.</summary>
    public decimal PriceCharged { get; set; }
    public GarmentStatus Status { get; set; } = GarmentStatus.Dirty;
    public string? DamageEvidenceJson { get; set; }
    public string? PhotoUrl { get; set; }
    /// <summary>E.g. "Casillero A3", "Rack 2".</summary>
    public string? Location { get; set; }
    public List<GarmentModifier> Modifiers { get; set; } = [];
}
