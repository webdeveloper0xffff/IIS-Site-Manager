using IIS_Site_Manager.API.Data;
using IIS_Site_Manager.API.Data.Entities;
using IIS_Site_Manager.API.Models;
using IIS_Site_Manager.API.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IIS_Site_Manager.API;

public static class SecuritySmokeTests
{
    public static async Task<int> RunAsync()
    {
        try
        {
            TestPasswordHashingRoundTrip();
            TestAdminPasswordHashValidation();
            TestAdminConfigurationValidation();
            await TestCustomerRegistrationStoresOnlyHashAsync();
            await TestLegacyCustomerLoginMigratesPasswordAsync();

            Console.WriteLine("Security smoke tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    static void TestPasswordHashingRoundTrip()
    {
        var hashing = new PasswordHashingService();
        var hash = hashing.HashPassword("Pa$$w0rd!");

        Assert(hash.StartsWith("pbkdf2-sha256$", StringComparison.Ordinal), "Password hash should use the PBKDF2 format.");
        Assert(hashing.VerifyPassword("Pa$$w0rd!", hash), "Password hash should validate the original password.");
        Assert(!hashing.VerifyPassword("wrong-password", hash), "Password hash should reject an invalid password.");
    }

    static void TestAdminPasswordHashValidation()
    {
        var hashing = new PasswordHashingService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Username"] = "admin",
                ["Admin:PasswordHash"] = hashing.HashPassword("SuperSecret!"),
                ["Admin:JwtKey"] = "12345678901234567890123456789012"
            })
            .Build();

        var auth = new AdminAuthService(config, hashing);

        Assert(auth.ValidateCredentials("admin", "SuperSecret!"), "Admin auth should accept the configured password hash.");
        Assert(!auth.ValidateCredentials("admin", "bad"), "Admin auth should reject an invalid password.");
    }

    static void TestAdminConfigurationValidation()
    {
        var valid = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Username"] = "admin",
                ["Admin:PasswordHash"] = "pbkdf2-sha256$100000$AAAAAAAAAAAAAAAAAAAAAA==$BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB=",
                ["Admin:JwtKey"] = "12345678901234567890123456789012",
                ["ConnectionStrings:Default"] = "Server=localhost\\SQLEXPRESS;Database=Dummy;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
            })
            .Build();

        AdminSecurityConfiguration.Validate(valid);

        var invalid = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Username"] = "admin",
                ["Admin:PasswordHash"] = "<set-admin-password-hash>",
                ["Admin:JwtKey"] = "<set-admin-jwt-key>",
                ["ConnectionStrings:Default"] = "<set-sqlserver-connection-string>"
            })
            .Build();

        AssertThrows<InvalidOperationException>(() => AdminSecurityConfiguration.Validate(invalid), "Placeholder config should be rejected.");
    }

    static async Task TestCustomerRegistrationStoresOnlyHashAsync()
    {
        await using var scope = await TestDbScope.CreateAsync();
        var service = new HostingPlatformService(scope.Db, new PasswordHashingService());

        var result = service.RegisterCustomer(new RegisterCustomerRequest("user@example.com", "Pa$$w0rd!"));
        Assert(result.Success, "Customer registration should succeed.");

        var customer = await scope.Db.CustomerAccounts.SingleAsync(c => c.Email == "user@example.com");
        Assert(string.IsNullOrWhiteSpace(customer.Password), "New customers should not keep a plaintext password.");
        Assert(!string.IsNullOrWhiteSpace(customer.PasswordHash), "New customers should store a password hash.");
        Assert(customer.PasswordHashAlgorithm == "pbkdf2-sha256", "New customers should record the hash algorithm.");
    }

    static async Task TestLegacyCustomerLoginMigratesPasswordAsync()
    {
        await using var scope = await TestDbScope.CreateAsync();
        var passwordHashing = new PasswordHashingService();
        var customer = new CustomerAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = "legacy@example.com",
            Password = "LegacyPass123!",
            PasswordHash = string.Empty,
            PasswordHashAlgorithm = string.Empty,
            Status = "active",
            CreatedUtc = DateTime.UtcNow
        };

        scope.Db.CustomerAccounts.Add(customer);
        await scope.Db.SaveChangesAsync();

        var service = new HostingPlatformService(scope.Db, passwordHashing);
        var result = service.Login(new LoginRequest("legacy@example.com", "LegacyPass123!"));

        Assert(result.Success, "Legacy customer login should succeed with the old plaintext password.");

        var updated = await scope.Db.CustomerAccounts.SingleAsync(c => c.Id == customer.Id);
        Assert(string.IsNullOrWhiteSpace(updated.Password), "Legacy plaintext password should be cleared after login.");
        Assert(!string.IsNullOrWhiteSpace(updated.PasswordHash), "Legacy login should write a password hash.");
        Assert(updated.PasswordHashAlgorithm == "pbkdf2-sha256", "Legacy login should record the hash algorithm.");
        Assert(passwordHashing.VerifyPassword("LegacyPass123!", updated.PasswordHash), "Migrated password hash should validate the original password.");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    static void AssertThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    sealed class TestDbScope : IAsyncDisposable
    {
        TestDbScope(ControlPlaneDbContext db)
        {
            Db = db;
        }

        public ControlPlaneDbContext Db { get; }

        public static async Task<TestDbScope> CreateAsync()
        {
            var builder = new DbContextOptionsBuilder<ControlPlaneDbContext>();
            builder.UseSqlServer(BuildConnectionString());

            var db = new ControlPlaneDbContext(builder.Options);
            await db.Database.MigrateAsync();
            return new TestDbScope(db);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Db.Database.EnsureDeletedAsync();
            }
            finally
            {
                await Db.DisposeAsync();
            }
        }

        static string BuildConnectionString()
        {
            var databaseName = $"IISSiteManagerSecurityTests_{Guid.NewGuid():N}";
            var configured = Environment.GetEnvironmentVariable("SECURITY_TEST_SQL_CONNECTION");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                var builder = new SqlConnectionStringBuilder(configured)
                {
                    InitialCatalog = databaseName
                };

                return builder.ConnectionString;
            }

            return $"Server=localhost\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False";
        }
    }
}
