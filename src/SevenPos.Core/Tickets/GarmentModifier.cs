namespace SevenPos.Core.Tickets;

/// <summary>Doc: Ticket_Items_Modificadores. Extras sold on that exact garment.</summary>
public class GarmentModifier : SyncEntity
{
    public Guid GarmentId { get; set; }
    public Guid ModifierId { get; set; }
    public decimal ChargeApplied { get; set; }
}
