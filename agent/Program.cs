using IIS_Site_Manager.Agent;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agent"));
builder.Services.AddSingleton<SystemMetricsCollector>();
builder.Services.AddSingleton<IISProvisioner>();
builder.Services.AddHttpClient<ControlPlaneClient>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "IIS-Site-Manager-Agent";
});

var host = builder.Build();
host.Run();
