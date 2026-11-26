namespace SevenPos.Core.Organization;

/// <summary>Doc: Sucursal.</summary>
public class Branch : SyncEntity
{
    public required string Name { get; set; }
    /// <summary>Up to 5 comma-separated emails for shift/alert reports.</summary>
    public string? AlertEmails { get; set; }
}
