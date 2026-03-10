namespace IIS_Site_Manager.API.Data.Entities;

public class ProvisionJobEntity
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid HostedSiteId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string PayloadJson { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? StartedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
}
