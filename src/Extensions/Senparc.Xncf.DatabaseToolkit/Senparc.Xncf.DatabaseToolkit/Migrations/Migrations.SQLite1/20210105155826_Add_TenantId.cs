using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.DatabaseToolkit.Migrations.Migrations.SQLite
{
    public partial class Add_TenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "DatabaseToolkitDbConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DatabaseToolkitDbConfig");
        }
    }
}
