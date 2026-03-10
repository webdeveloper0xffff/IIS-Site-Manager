using System.Security.Claims;
using System.Text;
using IIS_Site_Manager.API;
using IIS_Site_Manager.API.Data;
using IIS_Site_Manager.API.Models;
using IIS_Site_Manager.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

if (args.Contains("--run-security-smoke-tests", StringComparer.Ordinal))
{
    Environment.ExitCode = await SecuritySmokeTests.RunAsync();
    return;
}

var hashPasswordArg = args.FirstOrDefault(arg => arg.StartsWith("--hash-password=", StringComparison.Ordinal));
if (hashPasswordArg is not null)
{
    Console.WriteLine(new PasswordHashingService().HashPassword(hashPasswordArg["--hash-password=".Length..]));
    return;
}

var hashPasswordIndex = Array.IndexOf(args, "--hash-password");
if (hashPasswordIndex >= 0 && hashPasswordIndex + 1 < args.Length)
{
    Console.WriteLine(new PasswordHashingService().HashPassword(args[hashPasswordIndex + 1]));
    return;
}

var builder = WebApplication.CreateBuilder(args);

AdminSecurityConfiguration.Validate(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var jwtKey = builder.Configuration["Admin:JwtKey"]!;
var jwtIssuer = builder.Configuration["Admin:JwtIssuer"] ?? "IIS-Site-Manager";
var jwtAudience = builder.Configuration["Admin:JwtAudience"] ?? "IIS-Site-Manager-Admin";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
    });
});

builder.Services.AddSingleton<IISService>();
builder.Services.AddSingleton<SystemMonitorService>();
builder.Services.AddSingleton<PasswordHashingService>();
builder.Services.AddSingleton<AdminAuthService>();
builder.Services.AddDbContext<ControlPlaneDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")!));
builder.Services.AddScoped<HostingPlatformService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SystemMonitorService>());
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
    db.Database.Migrate();
}

app.UseCors();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (Directory.Exists(wwwroot))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

static Guid? GetCustomerId(HttpContext ctx)
{
    var raw = ctx.Request.Headers["X-Customer-Id"].FirstOrDefault();
    if (Guid.TryParse(raw, out var id)) return id;
    return null;
}

app.MapGet("/api/metrics", (SystemMonitorService monitor) =>
{
    var (cpu, memPercent, memUsed, memTotal, bytesRecv, bytesSent) = monitor.GetMetrics();
    return new SystemMetrics(cpu, memPercent, memUsed, memTotal, bytesRecv, bytesSent, bytesRecv + bytesSent, DateTime.UtcNow);
});

app.MapGet("/metrics", (SystemMonitorService monitor) =>
{
    var (cpu, memPercent, memUsed, memTotal, bytesRecv, bytesSent) = monitor.GetMetrics();
    return new SystemMetrics(cpu, memPercent, memUsed, memTotal, bytesRecv, bytesSent, bytesRecv + bytesSent, DateTime.UtcNow);
}).WithName("GetMetrics");

app.MapPost("/api/nodes/register", (RegisterNodeRequest req, HostingPlatformService platform) =>
{
    var node = platform.RegisterNode(req);
    return Results.Ok(node);
});

app.MapPost("/api/metrics/ingest", (IngestMetricsRequest req, HostingPlatformService platform) =>
{
    var ok = platform.IngestMetrics(req);
    return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { success = false, message = "Node not found." });
});

app.MapPost("/api/auth/register", (RegisterCustomerRequest req, HostingPlatformService platform) =>
{
    var result = platform.RegisterCustomer(req);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/auth/login", (LoginRequest req, HostingPlatformService platform) =>
{
    var result = platform.Login(req);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/api/me/server", (HostingPlatformService platform, HttpContext ctx) =>
{
    var customerId = GetCustomerId(ctx);
    if (customerId == null) return Results.BadRequest(new { message = "Missing X-Customer-Id header." });

    var server = platform.GetAssignedServer(customerId.Value);
    return server is null ? Results.NotFound(new { message = "Assigned server not found." }) : Results.Ok(server);
});

app.MapGet("/api/sites", (HostingPlatformService platform, IISService iis, HttpContext ctx) =>
{
    var customerId = GetCustomerId(ctx);
    if (customerId == null) return Results.Ok(iis.ListSites());
    return Results.Ok(platform.GetCustomerSites(customerId.Value));
});

app.MapPost("/api/sites", (CreateSiteRequest req, HostingPlatformService platform, IISService iis, HttpContext ctx) =>
{
    var customerId = GetCustomerId(ctx);
    if (customerId == null)
    {
        var legacy = iis.CreateSite(req.SiteName, req.Domain, req.PhysicalPath, req.AppPoolName, req.Port);
        return legacy.Success ? Results.Ok(new { success = true, message = legacy.Message }) : Results.BadRequest(new { success = false, message = legacy.Message });
    }

    var created = platform.CreateHostedSite(customerId.Value, req);
    return created.Success
        ? Results.Ok(new { success = true, message = created.Message, site = created.Site })
        : Results.BadRequest(new { success = false, message = created.Message });
});

app.MapPost("/api/admin/login", (AdminLoginRequest req, AdminAuthService auth) =>
{
    if (!auth.ValidateCredentials(req.Username, req.Password))
        return Results.BadRequest(new AdminLoginResponse(false, "Invalid admin credentials.", null));

    var token = auth.GenerateToken(req.Username.Trim());
    return Results.Ok(new AdminLoginResponse(true, "Login success.", token));
});

var admin = app.MapGroup("/api/admin");
admin.RequireAuthorization("AdminOnly");

admin.MapGet("/summary", (HostingPlatformService platform) => Results.Ok(platform.GetAdminSummary()));

admin.MapGet("/nodes", (HostingPlatformService platform) => Results.Ok(platform.GetNodes()));

admin.MapGet("/customers", (HostingPlatformService platform) => Results.Ok(platform.GetAdminCustomers()));

admin.MapPost("/customers/{customerId:guid}/approve", (Guid customerId, HostingPlatformService platform) =>
{
    var result = platform.ApproveCustomer(customerId);
    return result.Success
        ? Results.Ok(new { success = true, message = result.Message, customer = result.Customer })
        : Results.BadRequest(new { success = false, message = result.Message });
});

admin.MapGet("/sites", (HostingPlatformService platform) => Results.Ok(platform.GetAdminSites()));

admin.MapPost("/sites", (AdminCreateSiteRequest req, HostingPlatformService platform) =>
{
    var created = platform.CreateHostedSiteAsAdmin(req);
    return created.Success
        ? Results.Ok(new { success = true, message = created.Message, site = created.Site })
        : Results.BadRequest(new { success = false, message = created.Message });
});

admin.MapGet("/jobs", (HostingPlatformService platform) => Results.Ok(platform.GetProvisionJobs()));

app.MapPost("/api/agent/jobs/next", (AgentJobPollRequest req, HostingPlatformService platform) =>
{
    var job = platform.DequeueNextProvisionJob(req.NodeId);
    return job == null ? Results.NoContent() : Results.Ok(job);
});

app.MapPost("/api/agent/jobs/{jobId:guid}/complete", (Guid jobId, AgentJobCompleteRequest req, HostingPlatformService platform) =>
{
    var ok = platform.CompleteProvisionJob(jobId, req.NodeId);
    return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { success = false, message = "Job not found." });
});

app.MapPost("/api/agent/jobs/{jobId:guid}/fail", (Guid jobId, AgentJobFailRequest req, HostingPlatformService platform) =>
{
    var ok = platform.FailProvisionJob(jobId, req.NodeId, req.Error);
    return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { success = false, message = "Job not found." });
});

app.MapPost("/api/waitlist/process", (HostingPlatformService platform) =>
    Results.Ok(platform.ProcessWaitlist()))
    .RequireAuthorization("AdminOnly");

app.MapGet("/api/waitlist", (HostingPlatformService platform) =>
    Results.Ok(platform.GetWaitlist()))
    .RequireAuthorization("AdminOnly");

app.MapGet("/api/audit", (HostingPlatformService platform) =>
    Results.Ok(platform.GetAudit()))
    .RequireAuthorization("AdminOnly");

app.MapGet("/api/nodes", (HostingPlatformService platform) =>
    Results.Ok(platform.GetNodes()))
    .RequireAuthorization("AdminOnly");

app.MapGet("/sites", (IISService iis) => iis.ListSites()).WithName("ListSites");

app.MapPost("/sites", (CreateSiteRequest req, IISService iis) =>
{
    var (success, message) = iis.CreateSite(req.SiteName, req.Domain, req.PhysicalPath, req.AppPoolName, req.Port);
    return success ? Results.Ok(new { success, message }) : Results.BadRequest(new { success, message });
}).WithName("CreateSite");

if (Directory.Exists(wwwroot))
    app.MapFallbackToFile("index.html");

app.Run();
