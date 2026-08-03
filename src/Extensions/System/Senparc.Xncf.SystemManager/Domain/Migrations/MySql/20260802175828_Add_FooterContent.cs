using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.SystemManager.Domain.Migrations.MySql
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
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "© 2026 Senparc")
                .Annotation("MySql:CharSet", "utf8mb4");
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
