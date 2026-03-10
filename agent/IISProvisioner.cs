using System.Text.Json;
using Microsoft.Web.Administration;

namespace IIS_Site_Manager.Agent;

public sealed class IISProvisioner
{
    public (bool Success, string Message) Execute(AgentProvisionJobResponse job)
    {
        if (!string.Equals(job.Type, "create_site", StringComparison.OrdinalIgnoreCase))
            return (false, $"Unsupported job type '{job.Type}'.");

        CreateSiteProvisionPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<CreateSiteProvisionPayload>(job.PayloadJson);
        }
        catch (Exception ex)
        {
            return (false, $"Invalid job payload: {ex.Message}");
        }

        if (payload == null)
            return (false, "Job payload is empty.");

        return CreateSite(payload);
    }

    static (bool Success, string Message) CreateSite(CreateSiteProvisionPayload payload)
    {
        if (!OperatingSystem.IsWindows())
            return (false, "IIS provisioning is only supported on Windows.");

        try
        {
            using var serverManager = new ServerManager();

            if (serverManager.Sites.Any(s => s.Name.Equals(payload.SiteName, StringComparison.OrdinalIgnoreCase)))
                return (false, $"Site '{payload.SiteName}' already exists.");

            if (serverManager.Sites.Any(s => s.Bindings.Any(b => b.Host.Equals(payload.Domain, StringComparison.OrdinalIgnoreCase))))
                return (false, $"Domain '{payload.Domain}' is already bound to another site.");

            var path = Path.GetFullPath(payload.PhysicalPath.Trim().TrimEnd(Path.DirectorySeparatorChar, '/', '\\'));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var pool = serverManager.ApplicationPools[payload.AppPoolName];
            if (pool == null)
            {
                pool = serverManager.ApplicationPools.Add(payload.AppPoolName);
                pool.ManagedRuntimeVersion = string.Empty;
                pool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
            }

            var siteId = (serverManager.Sites.Max(s => (long?)s.Id) ?? 0) + 1;
            var site = serverManager.Sites.Add(payload.SiteName, "http", $"*:{payload.Port}:{payload.Domain}", path);
            site.Id = siteId;
            site.ApplicationDefaults.ApplicationPoolName = payload.AppPoolName;
            site.ServerAutoStart = true;

            if (!payload.Domain.StartsWith("*", StringComparison.Ordinal))
                site.Bindings.Add($"*:{payload.Port}:www.{payload.Domain}", "http");

            serverManager.CommitChanges();

            if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
            {
                const string defaultHtml =
                    "<!DOCTYPE html><html><head><title>Welcome</title><meta charset=\"utf-8\"></head><body><h1>Site is ready</h1><p>Your IIS site is working.</p></body></html>";
                File.WriteAllText(Path.Combine(path, "index.html"), defaultHtml);
            }

            return (true, $"Provisioned site '{payload.SiteName}'.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to provision site: {ex.Message}");
        }
    }
}
