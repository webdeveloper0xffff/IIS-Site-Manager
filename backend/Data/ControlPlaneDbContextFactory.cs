using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IIS_Site_Manager.API.Data;

public class ControlPlaneDbContextFactory : IDesignTimeDbContextFactory<ControlPlaneDbContext>
{
    public ControlPlaneDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var conn = config.GetConnectionString("Default")
                   ?? throw new InvalidOperationException("Missing required configuration value 'ConnectionStrings:Default'.");

        var optionsBuilder = new DbContextOptionsBuilder<ControlPlaneDbContext>();
        optionsBuilder.UseSqlServer(conn);
        return new ControlPlaneDbContext(optionsBuilder.Options);
    }
}
