namespace IIS_Site_Manager.Agent;

public sealed class AgentOptions
{
    public string BackendBaseUrl { get; set; } = "http://localhost:5032";
    public string NodeName { get; set; } = Environment.MachineName;
    public string PublicHost { get; set; } = Environment.MachineName;
    public bool Enabled { get; set; } = true;
    public int MetricsIntervalSeconds { get; set; } = 15;
    public int JobPollIntervalSeconds { get; set; } = 10;
    public int RequestTimeoutSeconds { get; set; } = 10;
}
