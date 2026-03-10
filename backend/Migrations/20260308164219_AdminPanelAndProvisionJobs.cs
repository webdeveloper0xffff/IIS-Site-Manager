using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIS_Site_Manager.API.Migrations
{
    /// <inheritdoc />
    public partial class AdminPanelAndProvisionJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastProvisionError",
                table: "HostedSites",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvisioningStatus",
                table: "HostedSites",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedUtc",
                table: "CustomerAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CustomerAccounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProvisionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostedSiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvisionJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HostedSites_ProvisioningStatus_CreatedUtc",
                table: "HostedSites",
                columns: new[] { "ProvisioningStatus", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_Status_CreatedUtc",
                table: "CustomerAccounts",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProvisionJobs_HostedSiteId",
                table: "ProvisionJobs",
                column: "HostedSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvisionJobs_NodeId_Status_CreatedUtc",
                table: "ProvisionJobs",
                columns: new[] { "NodeId", "Status", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProvisionJobs");

            migrationBuilder.DropIndex(
                name: "IX_HostedSites_ProvisioningStatus_CreatedUtc",
                table: "HostedSites");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAccounts_Status_CreatedUtc",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "LastProvisionError",
                table: "HostedSites");

            migrationBuilder.DropColumn(
                name: "ProvisioningStatus",
                table: "HostedSites");

            migrationBuilder.DropColumn(
                name: "ApprovedUtc",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CustomerAccounts");
        }
    }
}
