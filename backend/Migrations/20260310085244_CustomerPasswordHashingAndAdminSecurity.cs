using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIS_Site_Manager.API.Migrations
{
    /// <inheritdoc />
    public partial class CustomerPasswordHashingAndAdminSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "CustomerAccounts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "CustomerAccounts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHashAlgorithm",
                table: "CustomerAccounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "PasswordHashAlgorithm",
                table: "CustomerAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "CustomerAccounts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
