using IIS_Site_Manager.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IIS_Site_Manager.API.Data;

public class ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options) : DbContext(options)
{
    public DbSet<ServerNodeEntity> ServerNodes => Set<ServerNodeEntity>();
    public DbSet<CustomerAccountEntity> CustomerAccounts => Set<CustomerAccountEntity>();
    public DbSet<WaitlistEntryEntity> WaitlistEntries => Set<WaitlistEntryEntity>();
    public DbSet<HostedSiteEntity> HostedSites => Set<HostedSiteEntity>();
    public DbSet<AuditLogEntryEntity> AuditLogs => Set<AuditLogEntryEntity>();
    public DbSet<ProvisionJobEntity> ProvisionJobs => Set<ProvisionJobEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServerNodeEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NodeName).HasMaxLength(120).IsRequired();
            e.Property(x => x.PublicHost).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.NodeName).IsUnique();
        });

        modelBuilder.Entity<CustomerAccountEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.Password).HasMaxLength(255);
            e.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(x => x.PasswordHashAlgorithm).HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => new { x.Status, x.CreatedUtc });
        });

        modelBuilder.Entity<WaitlistEntryEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<HostedSiteEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SiteName).HasMaxLength(120).IsRequired();
            e.Property(x => x.Domain).HasMaxLength(255).IsRequired();
            e.Property(x => x.PhysicalPath).HasMaxLength(500).IsRequired();
            e.Property(x => x.AppPoolName).HasMaxLength(120).IsRequired();
            e.Property(x => x.FtpHost).HasMaxLength(255).IsRequired();
            e.Property(x => x.FtpUser).HasMaxLength(120).IsRequired();
            e.Property(x => x.FtpPassword).HasMaxLength(255).IsRequired();
            e.Property(x => x.WebDeployEndpoint).HasMaxLength(500).IsRequired();
            e.Property(x => x.DeployUser).HasMaxLength(120).IsRequired();
            e.Property(x => x.DeployPassword).HasMaxLength(255).IsRequired();
            e.Property(x => x.ProvisioningStatus).HasMaxLength(32).IsRequired();
            e.Property(x => x.LastProvisionError).HasMaxLength(2000);
            e.HasIndex(x => x.Domain).IsUnique();
            e.HasIndex(x => new { x.CustomerId, x.CreatedUtc });
            e.HasIndex(x => new { x.ProvisioningStatus, x.CreatedUtc });
        });

        modelBuilder.Entity<AuditLogEntryEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(120).IsRequired();
            e.Property(x => x.Actor).HasMaxLength(120).IsRequired();
            e.Property(x => x.Details).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => x.TimestampUtc);
        });

        modelBuilder.Entity<ProvisionJobEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.Property(x => x.PayloadJson).HasMaxLength(8000).IsRequired();
            e.Property(x => x.Error).HasMaxLength(2000);
            e.HasIndex(x => new { x.NodeId, x.Status, x.CreatedUtc });
            e.HasIndex(x => x.HostedSiteId);
        });
    }
}
