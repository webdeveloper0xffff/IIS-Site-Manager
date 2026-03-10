namespace IIS_Site_Manager.API.Data.Entities;

public class HostedSiteEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ServerNodeId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string PhysicalPath { get; set; } = string.Empty;
    public string AppPoolName { get; set; } = string.Empty;
    public int Port { get; set; }
    public string FtpHost { get; set; } = string.Empty;
    public string FtpUser { get; set; } = string.Empty;
    public string FtpPassword { get; set; } = string.Empty;
    public string WebDeployEndpoint { get; set; } = string.Empty;
    public string DeployUser { get; set; } = string.Empty;
    public string DeployPassword { get; set; } = string.Empty;
    public string ProvisioningStatus { get; set; } = "pending";
    public string? LastProvisionError { get; set; }
    public DateTime CreatedUtc { get; set; }
}
