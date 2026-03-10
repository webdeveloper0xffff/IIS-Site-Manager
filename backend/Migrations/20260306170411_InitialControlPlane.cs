using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIS_Site_Manager.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialControlPlane : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedServerNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HostedSites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhysicalPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AppPoolName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    FtpHost = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FtpUser = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FtpPassword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    WebDeployEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeployUser = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DeployPassword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostedSites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PublicHost = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    ReportedIisSiteCount = table.Column<int>(type: "int", nullable: false),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "float", nullable: false),
                    MemoryUsagePercent = table.Column<double>(type: "float", nullable: false),
                    BytesTotalPerSec = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TimestampUtc",
                table: "AuditLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_Email",
                table: "CustomerAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HostedSites_CustomerId_CreatedUtc",
                table: "HostedSites",
                columns: new[] { "CustomerId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HostedSites_Domain",
                table: "HostedSites",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerNodes_NodeName",
                table: "ServerNodes",
                column: "NodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_Email",
                table: "WaitlistEntries",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CustomerAccounts");

            migrationBuilder.DropTable(
                name: "HostedSites");

            migrationBuilder.DropTable(
                name: "ServerNodes");

            migrationBuilder.DropTable(
                name: "WaitlistEntries");
        }
    }
}
