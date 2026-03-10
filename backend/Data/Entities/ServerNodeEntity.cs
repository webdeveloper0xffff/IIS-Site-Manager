namespace IIS_Site_Manager.API.Data.Entities;

public class ServerNodeEntity
{
    public Guid Id { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string PublicHost { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool IsOnline { get; set; }
    public int ReportedIisSiteCount { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double BytesTotalPerSec { get; set; }
}
