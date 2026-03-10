using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace IIS_Site_Manager.Agent;

public sealed class ControlPlaneClient(HttpClient httpClient, IOptions<AgentOptions> options)
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    readonly AgentOptions _options = options.Value;

    public async Task<ServerNodeResponse?> RegisterNodeAsync(CancellationToken cancellationToken)
    {
        ConfigureClient();

        var request = new RegisterNodeRequest(
            string.IsNullOrWhiteSpace(_options.NodeName) ? Environment.MachineName : _options.NodeName.Trim(),
            string.IsNullOrWhiteSpace(_options.PublicHost) ? Environment.MachineName : _options.PublicHost.Trim(),
            _options.Enabled
        );

        using var response = await httpClient.PostAsJsonAsync("/api/nodes/register", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ServerNodeResponse>(JsonOptions, cancellationToken);
    }

    public async Task<bool> IngestMetricsAsync(Guid nodeId, MetricsSnapshot metrics, CancellationToken cancellationToken)
    {
        ConfigureClient();

        var request = new IngestMetricsRequest(
            nodeId,
            metrics.CpuUsagePercent,
            metrics.MemoryUsagePercent,
            metrics.BytesTotalPerSec,
            metrics.IisSiteCount,
            metrics.IsOnline
        );

        using var response = await httpClient.PostAsJsonAsync("/api/metrics/ingest", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<AgentProvisionJobResponse?> PollNextJobAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        ConfigureClient();

        using var response = await httpClient.PostAsJsonAsync("/api/agent/jobs/next", new AgentJobPollRequest(nodeId), cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AgentProvisionJobResponse>(JsonOptions, cancellationToken);
    }

    public async Task<bool> CompleteJobAsync(Guid jobId, Guid nodeId, CancellationToken cancellationToken)
    {
        ConfigureClient();

        using var response = await httpClient.PostAsJsonAsync($"/api/agent/jobs/{jobId}/complete", new AgentJobCompleteRequest(nodeId), cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> FailJobAsync(Guid jobId, Guid nodeId, string error, CancellationToken cancellationToken)
    {
        ConfigureClient();

        using var response = await httpClient.PostAsJsonAsync(
            $"/api/agent/jobs/{jobId}/fail",
            new AgentJobFailRequest(nodeId, error),
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    void ConfigureClient()
    {
        if (httpClient.BaseAddress is null)
        {
            var baseUrl = (_options.BackendBaseUrl ?? string.Empty).Trim();
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(_options.RequestTimeoutSeconds, 1));
        }
    }
}
