namespace SevenPos.Core.Entities;

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

public enum GarmentStatus { Dirty, InTransit, Washed, Ironed, Assembled, Delivered }

/// <summary>Doc: Ticket_Items_Pulverizado. One row per physical garment.</summary>
public class Garment : SyncEntity
{
    /// <summary>[BRANCH]-[YYYYMMDD]-[FOLIO]-[SEQ], printed on the label and scanned.</summary>
    public required string Code { get; set; }
    public Guid TicketId { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    /// <summary>Base price + modifiers at the moment of sale.</summary>
    public decimal PriceCharged { get; set; }
    public GarmentStatus Status { get; set; } = GarmentStatus.Dirty;
    public string? DamageEvidenceJson { get; set; }
    public string? PhotoUrl { get; set; }
    /// <summary>E.g. "Casillero A3", "Rack 2".</summary>
    public string? Location { get; set; }
    public List<GarmentModifier> Modifiers { get; set; } = [];
}

/// <summary>Doc: Ticket_Items_Modificadores. Extras sold on that exact garment.</summary>
public class GarmentModifier : SyncEntity
{
    public Guid GarmentId { get; set; }
    public Guid ModifierId { get; set; }
    public decimal ChargeApplied { get; set; }
}

/// <summary>Doc: Naturaleza (Cobro_Anticipado / Liquidacion).</summary>
public enum PaymentType { Advance, Settlement }
public enum PaymentMethod { Cash, Card, Wallet, PaymentLink }

/// <summary>Doc: Transacciones_Pagos.</summary>
public class Payment : SyncEntity
{
    public Guid TicketId { get; set; }
    /// <summary>Shift in which the money came in (for the shift close report).</summary>
    public Guid ShiftId { get; set; }
    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
}
