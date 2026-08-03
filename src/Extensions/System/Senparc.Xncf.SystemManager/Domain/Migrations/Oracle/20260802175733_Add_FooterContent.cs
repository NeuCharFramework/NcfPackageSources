using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.SystemManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_FooterContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FooterContent",
                table: "SystemConfigs",
                type: "NVARCHAR2(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "© 2026 Senparc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FooterContent",
                table: "SystemConfigs");
        }
    }
}
