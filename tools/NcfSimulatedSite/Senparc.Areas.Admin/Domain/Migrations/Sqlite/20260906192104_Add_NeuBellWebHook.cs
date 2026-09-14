using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_NeuBellWebHook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_NeuBellWebHook",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    WebHookUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ProviderFilter = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Secret = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    NotifyOnAdd = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyOnRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdminUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuBellWebHook", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuBellWebHook_ProviderFilter",
                table: "ADMIN_NeuBellWebHook",
                column: "ProviderFilter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuBellWebHook");
        }
    }
}
