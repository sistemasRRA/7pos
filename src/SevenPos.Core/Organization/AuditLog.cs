namespace SevenPos.Core.Organization;

public class AuditLog : SyncEntity
{
    public Guid? UserId { get; set; }
    public Guid BranchId { get; set; }
    /// <summary>E.g. TICKET_CANCELLED, PRICE_CHANGED, DRAWER_OPENED.</summary>
    public required string Action { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
