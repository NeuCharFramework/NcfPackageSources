using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Menu.Domain.Migrations.Oracle
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SysButtons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    MenuId = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    ButtonName = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    OpearMark = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    Url = table.Column<string>(type: "NVARCHAR2(350)", maxLength: 350, nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysButtons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysMenus",
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    MenuName = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    ParentId = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    Url = table.Column<string>(type: "NVARCHAR2(350)", maxLength: 350, nullable: true),
                    Icon = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    IsLocked = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    MenuType = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ResourceCode = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: true),
                    Sort = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Visible = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysMenus", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SysButtons");

            migrationBuilder.DropTable(
                name: "SysMenus");
        }
    }
}
