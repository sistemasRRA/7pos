namespace SevenPos.Core.Cash;

/// <summary>
/// Doc: Cajas_Turnos. One cashier shift on the branch's cash drawer, from opening
/// (opening cash) to the close (Corte Z). A branch has at most one open shift.
/// Totals per payment method are NOT stored here; they are computed from the
/// shift's Payments (ADR 0007). Rules: docs/features/shifts.md.
/// </summary>
public class Shift : SyncEntity
{
    public Guid BranchId { get; set; }
    /// <summary>Who opened the shift.</summary>
    public Guid UserId { get; set; }
    /// <summary>Doc: Fondo_Inicial. Cash in the drawer when the shift opens.</summary>
    public decimal OpeningCash { get; set; }
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Empty while the shift is open.</summary>
    public DateTime? ClosedAtUtc { get; set; }
    public Guid? ClosedByUserId { get; set; }
    /// <summary>Cash the cashier counts BEFORE seeing the expected amount.</summary>
    public decimal? BlindCashCount { get; set; }
    /// <summary>Counted − expected at the close. Negative = short, positive = over.</summary>
    public decimal? CashDifference { get; set; }

    public bool IsOpen => ClosedAtUtc is null;

    public List<Payment> Payments { get; set; } = [];
    public List<CashWithdrawal> Withdrawals { get; set; } = [];
}
