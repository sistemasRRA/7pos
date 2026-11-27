namespace SevenPos.Core.Cash;

/// <summary>Doc: Naturaleza. Advance = taken when the ticket is created (Cobro_Anticipado); Settlement = taken later, usually at pickup (Liquidacion).</summary>
public enum PaymentType { Advance, Settlement }

/// <summary>Wallet and PaymentLink arrive in Phase 3; today only Cash and Card are accepted.</summary>
public enum PaymentMethod { Cash, Card, Wallet, PaymentLink }

/// <summary>Doc: Transacciones_Pagos. Rules: docs/features/payments.md.</summary>
public class Payment : SyncEntity
{
    public Guid TicketId { get; set; }
    /// <summary>Shift in which the money came in (for the shift close report). Always an open shift of the ticket's branch.</summary>
    public Guid ShiftId { get; set; }
    /// <summary>Who received the money.</summary>
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
}
