namespace SevenPos.Core.Tickets;

public enum TicketStatus { Received, InProcess, Ready, OutForDelivery, Delivered, Cancelled }

public enum TicketOrigin { Counter, CleanCom, SevenWash, SevenCleanDark }

/// <summary>Doc: Tickets_Cabecera.</summary>
public class Ticket : SyncEntity
{
    /// <summary>Real sequential number per branch.</summary>
    public int Folio { get; set; }
    /// <summary>Masked order number shown to the customer.</summary>
    public required string PublicOrderId { get; set; }
    public Guid BranchId { get; set; }
    public Guid GuestId { get; set; }
    public Guest? Guest { get; set; }
    public Guid CashierId { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }
    public decimal Discounts { get; set; }
    public decimal Total { get; set; }
    public decimal BalanceDue { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Received;
    public TicketOrigin Origin { get; set; } = TicketOrigin.Counter;
    public string? SignatureUrl { get; set; }
    public string? CancellationReason { get; set; }

    public List<Garment> Garments { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}
