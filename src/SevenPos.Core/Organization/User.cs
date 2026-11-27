namespace SevenPos.Core.Organization;

/// <summary>Marketing manages the catalog and prices (ADR 0011).</summary>
public enum UserRole { Cashier, Manager, Admin, Marketing }

public class User : SyncEntity
{
    public required string Name { get; set; }
    public required string PinHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Cashier;
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>Wrong PINs in a row. Reset to 0 on a successful login.</summary>
    public int FailedPinAttempts { get; set; }
    /// <summary>While set and in the future, the user cannot log in.</summary>
    public DateTime? LockedUntilUtc { get; set; }
}
