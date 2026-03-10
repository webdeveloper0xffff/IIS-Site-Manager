using Microsoft.Extensions.Options;

namespace IIS_Site_Manager.Agent;

public class Worker(
    ILogger<Worker> logger,
    ControlPlaneClient controlPlaneClient,
    SystemMetricsCollector metricsCollector,
    IISProvisioner provisioner,
    IOptions<AgentOptions> options) : BackgroundService
{
    readonly AgentOptions _options = options.Value;
    Guid? _nodeId;
    DateTime _nextJobPollUtc = DateTime.UtcNow;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(_options.MetricsIntervalSeconds, 5));
        using var timer = new PeriodicTimer(interval);

        await EnsureRegisteredAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_nodeId is null)
                    await EnsureRegisteredAsync(stoppingToken);
                else
                {
                    await SendMetricsAsync(_nodeId.Value, stoppingToken);
                    await PollAndExecuteJobsAsync(_nodeId.Value, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Agent loop failed. Will retry on next cycle.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    async Task EnsureRegisteredAsync(CancellationToken cancellationToken)
    {
        try
        {
            var node = await controlPlaneClient.RegisterNodeAsync(cancellationToken);
            if (node is null)
            {
                logger.LogWarning("Node registration failed: backend returned non-success status.");
                return;
            }

            _nodeId = node.Id;
            logger.LogInformation("Node registered. NodeId={NodeId}, NodeName={NodeName}, Host={Host}", node.Id, node.NodeName, node.PublicHost);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Node registration failed.");
        }
    }

    async Task SendMetricsAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        var snapshot = metricsCollector.Collect();
        var success = await controlPlaneClient.IngestMetricsAsync(nodeId, snapshot, cancellationToken);

        if (!success)
        {
            logger.LogWarning("Metrics ingest failed for NodeId={NodeId}. Re-registering on next cycle.", nodeId);
            _nodeId = null;
            return;
        }

        logger.LogInformation(
            "Metrics sent. NodeId={NodeId}, CPU={Cpu:F2}%, MEM={Memory:F2}%, Net={Net:F2}B/s, IIS Sites={IisSites}",
            nodeId,
            snapshot.CpuUsagePercent,
            snapshot.MemoryUsagePercent,
            snapshot.BytesTotalPerSec,
            snapshot.IisSiteCount);
    }

    async Task PollAndExecuteJobsAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow < _nextJobPollUtc)
            return;

        _nextJobPollUtc = DateTime.UtcNow.AddSeconds(Math.Max(_options.JobPollIntervalSeconds, 5));

        var job = await controlPlaneClient.PollNextJobAsync(nodeId, cancellationToken);
        if (job is null)
            return;

        logger.LogInformation("Picked up job {JobId} of type {JobType}", job.Id, job.Type);

        var result = provisioner.Execute(job);
        if (result.Success)
        {
            var completed = await controlPlaneClient.CompleteJobAsync(job.Id, nodeId, cancellationToken);
            if (!completed)
                logger.LogWarning("Job {JobId} completed locally but backend completion call failed.", job.Id);
            else
                logger.LogInformation("Completed job {JobId}", job.Id);

            return;
        }

        var failed = await controlPlaneClient.FailJobAsync(job.Id, nodeId, result.Message, cancellationToken);
        if (!failed)
            logger.LogWarning("Job {JobId} failed locally but backend failure call failed: {Error}", job.Id, result.Message);
        else
            logger.LogWarning("Failed job {JobId}: {Error}", job.Id, result.Message);
    }
}
