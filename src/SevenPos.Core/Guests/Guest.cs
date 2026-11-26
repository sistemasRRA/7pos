namespace SevenPos.Core.Guests;

/// <summary>Doc: Invitado (customer). Phone (WhatsApp) is unique, but the key is the UUID.</summary>
public class Guest : SyncEntity
{
    /// <summary>10 digits, normalized with <see cref="PhoneNumber.TryNormalize"/>.</summary>
    public required string Phone { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public DateOnly? BirthDate { get; set; }
    public DateTime? LastVisitUtc { get; set; }

    /// <summary>
    /// The guest agreed to receive marketing (WhatsApp, email). Required by the Mexican
    /// data protection law (LFPDPPP) before sending any promotion.
    /// </summary>
    public bool MarketingConsent { get; set; }
    /// <summary>When consent was given; null if never given.</summary>
    public DateTime? MarketingConsentAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<GuestAddress> Addresses { get; set; } = [];
}
