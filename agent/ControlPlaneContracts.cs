namespace IIS_Site_Manager.Agent;

public record RegisterNodeRequest(
    string NodeName,
    string PublicHost,
    bool Enabled = true
);

public record IngestMetricsRequest(
    Guid NodeId,
    double CpuUsagePercent,
    double MemoryUsagePercent,
    double BytesTotalPerSec,
    int IisSiteCount,
    bool IsOnline = true
);

public record ServerNodeResponse(
    Guid Id,
    string NodeName,
    string PublicHost,
    bool Enabled,
    bool IsOnline,
    int ReportedIisSiteCount,
    DateTime LastHeartbeatUtc,
    double CpuUsagePercent,
    double MemoryUsagePercent,
    double BytesTotalPerSec
);

public record AgentJobPollRequest(
    Guid NodeId
);

public record AgentProvisionJobResponse(
    Guid Id,
    Guid HostedSiteId,
    string Type,
    string PayloadJson,
    DateTime LeaseUntilUtc
);

public record AgentJobCompleteRequest(
    Guid NodeId
);

public record AgentJobFailRequest(
    Guid NodeId,
    string Error
);

public record CreateSiteProvisionPayload(
    Guid HostedSiteId,
    string SiteName,
    string Domain,
    string PhysicalPath,
    string AppPoolName,
    int Port
);

public record MetricsSnapshot(
    double CpuUsagePercent,
    double MemoryUsagePercent,
    double BytesTotalPerSec,
    int IisSiteCount,
    bool IsOnline
);
