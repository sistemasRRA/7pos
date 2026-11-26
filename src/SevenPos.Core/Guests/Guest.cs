namespace SevenPos.Core.Guests;

/// <summary>Doc: Invitado (customer). Phone (WhatsApp) is unique, but the key is the UUID.</summary>
public class Guest : SyncEntity
{
    public required string Phone { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public DateOnly? BirthDate { get; set; }
    public DateTime? LastVisitUtc { get; set; }
    public List<GuestAddress> Addresses { get; set; } = [];
}
