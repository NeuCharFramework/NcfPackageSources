using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.SystemManager.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_NeuCharAccountInfo_To_SystemConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NeuCharAppKey",
                table: "SystemConfigs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NeuCharAppSecret",
                table: "SystemConfigs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeuCharDeveloperId",
                table: "SystemConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeuCharAppKey",
                table: "SystemConfigs");

            migrationBuilder.DropColumn(
                name: "NeuCharAppSecret",
                table: "SystemConfigs");

            migrationBuilder.DropColumn(
                name: "NeuCharDeveloperId",
                table: "SystemConfigs");
        }
    }
}
