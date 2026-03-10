using System.Text.Json;
using IIS_Site_Manager.API.Data;
using IIS_Site_Manager.API.Data.Entities;
using IIS_Site_Manager.API.Models;
using Microsoft.EntityFrameworkCore;

namespace IIS_Site_Manager.API.Services;

public class HostingPlatformService(ControlPlaneDbContext db, PasswordHashingService passwordHashing)
{
    public const int MaxSitesPerServer = 10;
    const string CustomerPending = "pending";
    const string CustomerActive = "active";
    const string SitePending = "pending";
    const string SiteSucceeded = "succeeded";
    const string SiteFailed = "failed";
    const string JobTypeCreateSite = "create_site";
    const string JobPending = "pending";
    const string JobRunning = "running";
    const string JobSucceeded = "succeeded";
    const string JobFailed = "failed";

    public ServerNode RegisterNode(RegisterNodeRequest req)
    {
        var nodeName = req.NodeName.Trim();
        var host = req.PublicHost.Trim();
        var now = DateTime.UtcNow;
        var existing = db.ServerNodes.FirstOrDefault(n => n.NodeName.ToLower() == nodeName.ToLower());

        if (existing != null)
        {
            existing.PublicHost = host;
            existing.Enabled = req.Enabled;
            existing.IsOnline = true;
            existing.LastHeartbeatUtc = now;
            db.SaveChanges();
            AddAudit("node.register", "agent", $"Updated node {existing.NodeName} ({existing.Id})");
            return ToModel(existing);
        }

        var created = new ServerNodeEntity
        {
            Id = Guid.NewGuid(),
            NodeName = nodeName,
            PublicHost = host,
            Enabled = req.Enabled,
            IsOnline = true,
            LastHeartbeatUtc = now
        };
        db.ServerNodes.Add(created);
        db.SaveChanges();
        AddAudit("node.register", "agent", $"Registered node {created.NodeName} ({created.Id})");
        return ToModel(created);
    }

    public bool IngestMetrics(IngestMetricsRequest req)
    {
        var node = db.ServerNodes.FirstOrDefault(n => n.Id == req.NodeId);
        if (node == null) return false;

        node.CpuUsagePercent = req.CpuUsagePercent;
        node.MemoryUsagePercent = req.MemoryUsagePercent;
        node.BytesTotalPerSec = req.BytesTotalPerSec;
        node.ReportedIisSiteCount = Math.Max(req.IisSiteCount, 0);
        node.IsOnline = req.IsOnline;
        node.LastHeartbeatUtc = DateTime.UtcNow;
        db.SaveChanges();
        return true;
    }

    public RegisterCustomerResponse RegisterCustomer(RegisterCustomerRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
            return new RegisterCustomerResponse(false, "Email and password are required.", false, null, null);

        if (db.CustomerAccounts.Any(c => c.Email == email))
            return new RegisterCustomerResponse(false, "Email already exists.", false, null, null);

        var customer = new CustomerAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = null,
            PasswordHash = passwordHashing.HashPassword(req.Password),
            PasswordHashAlgorithm = "pbkdf2-sha256",
            Status = CustomerPending,
            CreatedUtc = DateTime.UtcNow
        };

        db.CustomerAccounts.Add(customer);
        db.SaveChanges();
        AddAudit("customer.register", customer.Id.ToString(), $"Registered pending customer {email}");
        return new RegisterCustomerResponse(true, "Registration submitted for review.", false, customer.Id, null);
    }

    public LoginResponse Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var customer = db.CustomerAccounts.FirstOrDefault(c =>
            c.Email == email &&
            c.Status == CustomerActive);

        if (customer == null)
            return new LoginResponse(false, "Invalid credentials.", null);

        if (!string.IsNullOrWhiteSpace(customer.PasswordHash))
        {
            var ok = passwordHashing.VerifyPassword(req.Password, customer.PasswordHash);
            return ok
                ? new LoginResponse(true, "Login success.", customer.Id)
                : new LoginResponse(false, "Invalid credentials.", null);
        }

        var legacyOk = !string.IsNullOrWhiteSpace(customer.Password) &&
                       string.Equals(customer.Password, req.Password, StringComparison.Ordinal);

        if (!legacyOk)
            return new LoginResponse(false, "Invalid credentials.", null);

        customer.PasswordHash = passwordHashing.HashPassword(req.Password);
        customer.PasswordHashAlgorithm = "pbkdf2-sha256";
        customer.Password = null;
        db.SaveChanges();
        AddAudit("customer.password.migrated", customer.Id.ToString(), $"Migrated legacy password for {customer.Email}");

        return new LoginResponse(true, "Login success.", customer.Id);
    }

    public ServerNode? GetAssignedServer(Guid customerId)
    {
        var customer = db.CustomerAccounts.FirstOrDefault(c => c.Id == customerId && c.Status == CustomerActive);
        if (customer?.AssignedServerNodeId == null) return null;

        var node = db.ServerNodes.FirstOrDefault(n => n.Id == customer.AssignedServerNodeId.Value);
        return node == null ? null : ToModel(node);
    }

    public (bool Success, string Message, HostedSite? Site) CreateHostedSite(Guid customerId, CreateSiteRequest req)
    {
        var customer = db.CustomerAccounts.FirstOrDefault(c => c.Id == customerId && c.Status == CustomerActive);
        if (customer == null) return (false, "Customer not found or not active.", null);

        return CreateHostedSiteInternal(customer, new AdminCreateSiteRequest(
            customerId,
            req.SiteName,
            req.Domain,
            req.PhysicalPath,
            req.AppPoolName,
            req.Port));
    }

    public (bool Success, string Message, HostedSite? Site) CreateHostedSiteAsAdmin(AdminCreateSiteRequest req)
    {
        var customer = db.CustomerAccounts.FirstOrDefault(c => c.Id == req.CustomerId);
        if (customer == null) return (false, "Customer not found.", null);
        if (customer.Status != CustomerActive) return (false, "Customer must be approved before provisioning a site.", null);

        return CreateHostedSiteInternal(customer, req);
    }

    public List<HostedSite> GetCustomerSites(Guid customerId)
    {
        return db.HostedSites
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.CreatedUtc)
            .AsEnumerable()
            .Select(ToModel)
            .ToList();
    }

    public ProcessWaitlistResponse ProcessWaitlist()
    {
        var entries = db.WaitlistEntries.OrderBy(w => w.CreatedUtc).ToList();
        var processed = 0;
        var assigned = 0;

        foreach (var entry in entries)
        {
            processed++;
            var node = PickAvailableNode();
            if (node == null) break;

            var customer = db.CustomerAccounts.FirstOrDefault(c => c.Email == entry.Email);
            if (customer == null) continue;

            customer.AssignedServerNodeId = node.Id;
            customer.Status = CustomerActive;
            customer.ApprovedUtc = DateTime.UtcNow;
            db.WaitlistEntries.Remove(entry);
            db.SaveChanges();
            assigned++;
            AddAudit("waitlist.assigned", "admin", $"Assigned {entry.Email} to {node.NodeName}");
        }

        var remaining = db.WaitlistEntries.Count();
        return new ProcessWaitlistResponse(processed, assigned, remaining);
    }

    public List<ServerNode> GetNodes()
    {
        return db.ServerNodes.OrderBy(n => n.NodeName).AsEnumerable().Select(ToModel).ToList();
    }

    public List<WaitlistEntry> GetWaitlist()
    {
        return db.WaitlistEntries
            .OrderBy(w => w.CreatedUtc)
            .AsEnumerable()
            .Select(w => new WaitlistEntry(w.Id, w.Email, w.CreatedUtc, w.Reason))
            .ToList();
    }

    public List<AuditLogEntry> GetAudit()
    {
        return db.AuditLogs
            .OrderByDescending(a => a.TimestampUtc)
            .Take(200)
            .AsEnumerable()
            .Select(a => new AuditLogEntry(a.Id, a.TimestampUtc, a.Action, a.Actor, a.Details))
            .ToList();
    }

    public AdminSummaryResponse GetAdminSummary()
    {
        return new AdminSummaryResponse(
            db.ServerNodes.Count(n => n.Enabled && n.IsOnline),
            db.CustomerAccounts.Count(c => c.Status == CustomerPending),
            db.CustomerAccounts.Count(c => c.Status == CustomerActive),
            db.HostedSites.Count(s => s.ProvisioningStatus == SitePending),
            db.ProvisionJobs.Count(j => j.Status == JobFailed));
    }

    public List<AdminCustomerView> GetAdminCustomers()
    {
        var nodes = db.ServerNodes.ToDictionary(n => n.Id, n => n.NodeName);
        return db.CustomerAccounts
            .OrderByDescending(c => c.CreatedUtc)
            .AsEnumerable()
            .Select(c => new AdminCustomerView(
                c.Id,
                c.Email,
                c.Status,
                c.CreatedUtc,
                c.ApprovedUtc,
                c.AssignedServerNodeId,
                c.AssignedServerNodeId != null && nodes.TryGetValue(c.AssignedServerNodeId.Value, out var nodeName) ? nodeName : null))
            .ToList();
    }

    public (bool Success, string Message, AdminCustomerView? Customer) ApproveCustomer(Guid customerId)
    {
        var customer = db.CustomerAccounts.FirstOrDefault(c => c.Id == customerId);
        if (customer == null) return (false, "Customer not found.", null);
        if (customer.Status == CustomerActive) return (true, "Customer already active.", ToAdminCustomer(customer));

        var node = PickAvailableNode();
        if (node == null)
        {
            if (!db.WaitlistEntries.Any(w => w.Email == customer.Email))
            {
                db.WaitlistEntries.Add(new WaitlistEntryEntity
                {
                    Id = Guid.NewGuid(),
                    Email = customer.Email,
                    CreatedUtc = DateTime.UtcNow,
                    Reason = "No server capacity during admin approval"
                });
                db.SaveChanges();
            }

            AddAudit("customer.approve.blocked", "admin", $"No capacity for {customer.Email}");
            return (false, "No available server capacity.", null);
        }

        customer.Status = CustomerActive;
        customer.AssignedServerNodeId = node.Id;
        customer.ApprovedUtc = DateTime.UtcNow;
        db.SaveChanges();
        AddAudit("customer.approve", "admin", $"Approved {customer.Email} on node {node.NodeName}");
        return (true, "Customer approved.", ToAdminCustomer(customer, node.NodeName));
    }

    public List<AdminSiteView> GetAdminSites()
    {
        var customers = db.CustomerAccounts.ToDictionary(c => c.Id, c => c.Email);
        var nodes = db.ServerNodes.ToDictionary(n => n.Id, n => n.NodeName);

        return db.HostedSites
            .OrderByDescending(s => s.CreatedUtc)
            .AsEnumerable()
            .Select(s => new AdminSiteView(
                s.Id,
                s.CustomerId,
                customers.GetValueOrDefault(s.CustomerId, "unknown"),
                s.ServerNodeId,
                nodes.GetValueOrDefault(s.ServerNodeId, "unknown"),
                s.SiteName,
                s.Domain,
                s.PhysicalPath,
                s.AppPoolName,
                s.Port,
                s.ProvisioningStatus,
                s.LastProvisionError,
                new PublishCredentials(s.FtpHost, s.FtpUser, s.FtpPassword, s.WebDeployEndpoint, s.DeployUser, s.DeployPassword),
                s.CreatedUtc))
            .ToList();
    }

    public List<ProvisionJob> GetProvisionJobs()
    {
        return db.ProvisionJobs
            .OrderByDescending(j => j.CreatedUtc)
            .Take(200)
            .AsEnumerable()
            .Select(ToJobModel)
            .ToList();
    }

    public AgentProvisionJobResponse? DequeueNextProvisionJob(Guid nodeId)
    {
        var now = DateTime.UtcNow;
        var node = db.ServerNodes.FirstOrDefault(n => n.Id == nodeId && n.Enabled && n.IsOnline);
        if (node == null) return null;

        var job = db.ProvisionJobs
            .Where(j => j.NodeId == nodeId &&
                        (j.Status == JobPending || (j.Status == JobRunning && j.LeaseUntilUtc != null && j.LeaseUntilUtc < now)))
            .OrderBy(j => j.CreatedUtc)
            .FirstOrDefault();

        if (job == null) return null;

        job.Status = JobRunning;
        job.StartedUtc ??= now;
        job.LeaseUntilUtc = now.AddMinutes(2);
        db.SaveChanges();

        return new AgentProvisionJobResponse(job.Id, job.HostedSiteId, job.Type, job.PayloadJson, job.LeaseUntilUtc.Value);
    }

    public bool CompleteProvisionJob(Guid jobId, Guid nodeId)
    {
        var job = db.ProvisionJobs.FirstOrDefault(j => j.Id == jobId && j.NodeId == nodeId);
        if (job == null) return false;

        var site = db.HostedSites.FirstOrDefault(s => s.Id == job.HostedSiteId);
        if (site == null) return false;

        job.Status = JobSucceeded;
        job.CompletedUtc = DateTime.UtcNow;
        job.LeaseUntilUtc = null;
        job.Error = null;

        site.ProvisioningStatus = SiteSucceeded;
        site.LastProvisionError = null;

        db.SaveChanges();
        AddAudit("job.complete", "agent", $"Completed {job.Type} for site {site.SiteName}");
        return true;
    }

    public bool FailProvisionJob(Guid jobId, Guid nodeId, string error)
    {
        var job = db.ProvisionJobs.FirstOrDefault(j => j.Id == jobId && j.NodeId == nodeId);
        if (job == null) return false;

        var site = db.HostedSites.FirstOrDefault(s => s.Id == job.HostedSiteId);
        if (site == null) return false;

        job.Status = JobFailed;
        job.CompletedUtc = DateTime.UtcNow;
        job.LeaseUntilUtc = null;
        job.Error = Truncate(error, 2000);

        site.ProvisioningStatus = SiteFailed;
        site.LastProvisionError = Truncate(error, 2000);

        db.SaveChanges();
        AddAudit("job.fail", "agent", $"Failed {job.Type} for site {site.SiteName}: {job.Error}");
        return true;
    }

    (bool Success, string Message, HostedSite? Site) CreateHostedSiteInternal(CustomerAccountEntity customer, AdminCreateSiteRequest req)
    {
        if (customer.AssignedServerNodeId == null) return (false, "Customer has no assigned server.", null);

        var node = db.ServerNodes.FirstOrDefault(n => n.Id == customer.AssignedServerNodeId.Value);
        if (node == null) return (false, "Assigned server not found.", null);
        if (!node.Enabled || !node.IsOnline) return (false, "Assigned server is not available.", null);

        var domain = req.Domain.Trim().ToLowerInvariant();
        if (db.HostedSites.Any(s => s.Domain == domain))
            return (false, $"Domain '{req.Domain}' already exists.", null);

        var publish = GeneratePublishCredentials(node.PublicHost, req.SiteName);
        var siteId = Guid.NewGuid();
        var site = new HostedSiteEntity
        {
            Id = siteId,
            CustomerId = customer.Id,
            ServerNodeId = node.Id,
            SiteName = req.SiteName.Trim(),
            Domain = domain,
            PhysicalPath = req.PhysicalPath.Trim(),
            AppPoolName = string.IsNullOrWhiteSpace(req.AppPoolName) ? "DefaultAppPool" : req.AppPoolName.Trim(),
            Port = req.Port,
            FtpHost = publish.FtpHost,
            FtpUser = publish.FtpUser,
            FtpPassword = publish.FtpPassword,
            WebDeployEndpoint = publish.WebDeployEndpoint,
            DeployUser = publish.DeployUser,
            DeployPassword = publish.DeployPassword,
            ProvisioningStatus = SitePending,
            CreatedUtc = DateTime.UtcNow
        };

        var payload = JsonSerializer.Serialize(new CreateSiteProvisionPayload(
            site.Id,
            site.SiteName,
            site.Domain,
            site.PhysicalPath,
            site.AppPoolName,
            site.Port));

        var job = new ProvisionJobEntity
        {
            Id = Guid.NewGuid(),
            NodeId = node.Id,
            CustomerId = customer.Id,
            HostedSiteId = site.Id,
            Type = JobTypeCreateSite,
            Status = JobPending,
            PayloadJson = payload,
            CreatedUtc = DateTime.UtcNow
        };

        db.HostedSites.Add(site);
        db.ProvisionJobs.Add(job);
        db.SaveChanges();

        AddAudit("site.queue", "admin", $"Queued site {site.SiteName} for node {node.NodeName}");
        return (true, "Site provisioning job created.", ToModel(site));
    }

    ServerNodeEntity? PickAvailableNode()
    {
        var candidates = db.ServerNodes
            .Where(n => n.Enabled && n.IsOnline)
            .ToList()
            .Where(n => GetEffectiveIisSiteCount(n.Id, n.ReportedIisSiteCount) < MaxSitesPerServer)
            .ToList();

        if (candidates.Count == 0) return null;
        return candidates[Random.Shared.Next(candidates.Count)];
    }

    int GetEffectiveIisSiteCount(Guid nodeId, int reported)
    {
        var platformCount = db.HostedSites.Count(s => s.ServerNodeId == nodeId && s.ProvisioningStatus != SiteFailed);
        return Math.Max(reported, platformCount);
    }

    PublishCredentials GeneratePublishCredentials(string host, string siteName)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new PublishCredentials(
            host,
            $"{siteName}_ftp_{suffix}",
            Guid.NewGuid().ToString("N")[..16],
            $"https://{host}:8172/msdeploy.axd",
            $"{siteName}_deploy_{suffix}",
            Guid.NewGuid().ToString("N")[..16]
        );
    }

    void AddAudit(string action, string actor, string details)
    {
        db.AuditLogs.Add(new AuditLogEntryEntity
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            Action = action,
            Actor = actor,
            Details = Truncate(details, 2000)
        });
        db.SaveChanges();
    }

    AdminCustomerView ToAdminCustomer(CustomerAccountEntity customer, string? nodeName = null) => new(
        customer.Id,
        customer.Email,
        customer.Status,
        customer.CreatedUtc,
        customer.ApprovedUtc,
        customer.AssignedServerNodeId,
        nodeName ?? db.ServerNodes.Where(n => n.Id == customer.AssignedServerNodeId).Select(n => n.NodeName).FirstOrDefault());

    static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    static ServerNode ToModel(ServerNodeEntity e) => new(
        e.Id,
        e.NodeName,
        e.PublicHost,
        e.Enabled,
        e.IsOnline,
        e.ReportedIisSiteCount,
        e.LastHeartbeatUtc,
        e.CpuUsagePercent,
        e.MemoryUsagePercent,
        e.BytesTotalPerSec
    );

    static HostedSite ToModel(HostedSiteEntity e) => new(
        e.Id,
        e.CustomerId,
        e.ServerNodeId,
        e.SiteName,
        e.Domain,
        e.PhysicalPath,
        e.AppPoolName,
        e.Port,
        new PublishCredentials(e.FtpHost, e.FtpUser, e.FtpPassword, e.WebDeployEndpoint, e.DeployUser, e.DeployPassword),
        e.ProvisioningStatus,
        e.LastProvisionError,
        e.CreatedUtc
    );

    static ProvisionJob ToJobModel(ProvisionJobEntity e) => new(
        e.Id,
        e.NodeId,
        e.CustomerId,
        e.HostedSiteId,
        e.Type,
        e.Status,
        e.PayloadJson,
        e.Error,
        e.CreatedUtc,
        e.StartedUtc,
        e.CompletedUtc,
        e.LeaseUntilUtc
    );

    record CreateSiteProvisionPayload(
        Guid HostedSiteId,
        string SiteName,
        string Domain,
        string PhysicalPath,
        string AppPoolName,
        int Port
    );
}
