namespace SevenPos.Core.Cash;

/// <summary>
/// Doc: Cajas_Turnos (Corte X / Corte Z). Totals per payment method are
/// NOT stored here; they are computed from the shift's Payments.
/// </summary>
public class Shift : SyncEntity
{
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    /// <summary>Cash the cashier counts BEFORE seeing the expected amount.</summary>
    public decimal? BlindCashCount { get; set; }
    /// <summary>Negative = short, positive = over.</summary>
    public decimal? CashDifference { get; set; }

    public List<Payment> Payments { get; set; } = [];
    public List<CashWithdrawal> Withdrawals { get; set; } = [];
}
