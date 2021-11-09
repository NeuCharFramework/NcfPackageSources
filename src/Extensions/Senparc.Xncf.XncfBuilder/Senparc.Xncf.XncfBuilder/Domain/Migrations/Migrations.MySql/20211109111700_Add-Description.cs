using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.XncfBuilder.Domain.Migrations.Migrations.MySql
{
    public partial class AddDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "XncfBuilderConfig",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "XncfBuilderConfig");
        }
    }
}
