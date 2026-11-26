namespace SevenPos.Core.Cash;

/// <summary>Doc: Salidas de efectivo (supplier payments, expenses, cash pickups).</summary>
public class CashWithdrawal : SyncEntity
{
    public Guid ShiftId { get; set; }
    public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
