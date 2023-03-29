using Microsoft.EntityFrameworkCore.Migrations;

namespace Template_OrgName.Xncf.Template_XncfName.Migrations.Migrations.SqlServer
{
    public partial class AddTenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Template_OrgName_Template_XncfName_Color",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Template_OrgName_Template_XncfName_Color");
        }
    }
}
