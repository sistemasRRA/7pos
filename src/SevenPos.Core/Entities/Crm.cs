namespace SevenPos.Core.Entities;

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

public class GuestAddress : SyncEntity
{
    public Guid GuestId { get; set; }
    public string? Street { get; set; }
    /// <summary>Doc: Colonia.</summary>
    public string? Neighborhood { get; set; }
    public required string PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
