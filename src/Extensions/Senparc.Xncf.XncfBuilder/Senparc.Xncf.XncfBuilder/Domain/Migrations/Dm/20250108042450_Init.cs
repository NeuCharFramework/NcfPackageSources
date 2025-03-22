using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.XncfBuilder.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XncfBuilderConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    SlnFilePath = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    OrgName = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    XncfName = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    Version = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    MenuName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "NVARCHAR2(400)", maxLength: 400, nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfBuilderConfig", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XncfBuilderConfig");
        }
    }
}
