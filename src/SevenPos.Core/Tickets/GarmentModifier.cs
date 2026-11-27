namespace SevenPos.Core.Tickets;

/// <summary>Doc: Ticket_Items_Modificadores. Extras sold on that exact garment.</summary>
public class GarmentModifier : SyncEntity
{
    public Guid GarmentId { get; set; }
    public Guid ModifierId { get; set; }
    public Modifier? Modifier { get; set; }
    /// <summary>What this extra added to the garment's price, frozen at the moment of sale.</summary>
    public decimal ChargeApplied { get; set; }
}
