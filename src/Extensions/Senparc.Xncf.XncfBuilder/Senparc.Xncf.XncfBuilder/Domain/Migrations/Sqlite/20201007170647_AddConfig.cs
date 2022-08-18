using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.XncfBuilder.Domain.Migrations.Sqlite
{
    public partial class AddConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XncfBuilderConfig",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    SlnFilePath = table.Column<string>(maxLength: 300, nullable: true),
                    OrgName = table.Column<string>(maxLength: 300, nullable: true),
                    XncfName = table.Column<string>(maxLength: 50, nullable: true),
                    Version = table.Column<string>(maxLength: 100, nullable: true),
                    MenuName = table.Column<string>(maxLength: 100, nullable: true),
                    Icon = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfBuilderConfig", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XncfBuilderConfig");
        }
    }
}
