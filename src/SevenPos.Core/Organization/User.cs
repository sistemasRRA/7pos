namespace SevenPos.Core.Organization;

public enum UserRole { Cashier, Manager, Admin }

public class User : SyncEntity
{
    public required string Name { get; set; }
    public required string PinHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Cashier;
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
}
