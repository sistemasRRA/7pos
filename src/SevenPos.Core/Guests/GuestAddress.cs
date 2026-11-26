namespace SevenPos.Core.Guests;

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
