namespace IIS_Site_Manager.API.Models;

public record ServerNode(
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

public record CustomerAccount(
    Guid Id,
    string Email,
    string PasswordHashAlgorithm,
    string Status,
    DateTime CreatedUtc,
    DateTime? ApprovedUtc,
    Guid? AssignedServerNodeId
);

public record WaitlistEntry(
    Guid Id,
    string Email,
    DateTime CreatedUtc,
    string Reason
);

public record PublishCredentials(
    string FtpHost,
    string FtpUser,
    string FtpPassword,
    string WebDeployEndpoint,
    string DeployUser,
    string DeployPassword
);

public record HostedSite(
    Guid Id,
    Guid CustomerId,
    Guid ServerNodeId,
    string SiteName,
    string Domain,
    string PhysicalPath,
    string AppPoolName,
    int Port,
    PublishCredentials Publish,
    string ProvisioningStatus,
    string? LastProvisionError,
    DateTime CreatedUtc
);

public record AuditLogEntry(
    Guid Id,
    DateTime TimestampUtc,
    string Action,
    string Actor,
    string Details
);

public record ProvisionJob(
    Guid Id,
    Guid NodeId,
    Guid CustomerId,
    Guid HostedSiteId,
    string Type,
    string Status,
    string PayloadJson,
    string? Error,
    DateTime CreatedUtc,
    DateTime? StartedUtc,
    DateTime? CompletedUtc,
    DateTime? LeaseUntilUtc
);

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

public record RegisterCustomerRequest(
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record RegisterCustomerResponse(
    bool Success,
    string Message,
    bool Waitlisted,
    Guid? CustomerId,
    Guid? ServerNodeId
);

public record LoginResponse(
    bool Success,
    string Message,
    Guid? CustomerId
);

public record ProcessWaitlistResponse(
    int Processed,
    int Assigned,
    int Remaining
);

public record AdminLoginRequest(
    string Username,
    string Password
);

public record AdminLoginResponse(
    bool Success,
    string Message,
    string? Token
);

public record AdminSummaryResponse(
    int OnlineNodeCount,
    int PendingCustomerCount,
    int ActiveCustomerCount,
    int PendingSiteCount,
    int FailedJobCount
);

public record AdminCustomerView(
    Guid Id,
    string Email,
    string Status,
    DateTime CreatedUtc,
    DateTime? ApprovedUtc,
    Guid? AssignedServerNodeId,
    string? AssignedNodeName
);

public record AdminSiteView(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    Guid ServerNodeId,
    string NodeName,
    string SiteName,
    string Domain,
    string PhysicalPath,
    string AppPoolName,
    int Port,
    string ProvisioningStatus,
    string? LastProvisionError,
    PublishCredentials Publish,
    DateTime CreatedUtc
);

public record AdminCreateSiteRequest(
    Guid CustomerId,
    string SiteName,
    string Domain,
    string PhysicalPath,
    string AppPoolName = "DefaultAppPool",
    int Port = 80
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
