using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_NeuCharPivotBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotBoard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    PageKey = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Columns = table.Column<int>(type: "INT", nullable: false),
                    BlocksJson = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "BIT", nullable: false),
                    AdminUserId = table.Column<int>(type: "INT", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotBoard", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotBoard_PageKey",
                table: "ADMIN_NeuCharPivotBoard",
                column: "PageKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharPivotBoard");
        }
    }
}
