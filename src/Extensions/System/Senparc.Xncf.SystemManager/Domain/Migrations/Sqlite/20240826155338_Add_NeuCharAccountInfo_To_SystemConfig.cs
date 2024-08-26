using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.SystemManager.Domain.Migrations.Sqlite
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
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NeuCharAppSecret",
                table: "SystemConfigs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeuCharDeveloperId",
                table: "SystemConfigs",
                type: "INTEGER",
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
