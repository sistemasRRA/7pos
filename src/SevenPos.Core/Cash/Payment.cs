namespace SevenPos.Core.Cash;

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
