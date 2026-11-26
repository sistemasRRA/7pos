namespace SevenPos.Core.Common;

public enum SyncStatus { Pending = 0, Synced = 1, Error = 2 }

/// <summary>
/// Offline-first sync fields shared by every table.
/// The UUID (doc: ID_Global) is the primary key, so records created offline
/// in different branches never collide.
/// </summary>
public abstract class SyncEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    /// <summary>Soft delete.</summary>
    public bool IsActive { get; set; } = true;
}
