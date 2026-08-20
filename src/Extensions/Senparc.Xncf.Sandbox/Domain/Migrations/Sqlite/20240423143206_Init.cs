using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Sandbox.Domain.Migrations.Sqlite
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_Sandbox_SandboxSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),

                    SessionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OwnerUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RuntimeKind = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RuntimeHandle = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    HostPort = table.Column<int>(type: "INTEGER", nullable: true),
                    AccessUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AccessToken = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CpuLimit = table.Column<double>(type: "REAL", nullable: false),
                    MemoryMb = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StatusMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_Sandbox_SandboxSession", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_Sandbox_SandboxSession_ExpiresAtUtc",
                table: "Senparc_Sandbox_SandboxSession",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_Sandbox_SandboxSession_OwnerUserId_Status",
                table: "Senparc_Sandbox_SandboxSession",
                columns: new[] { "OwnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_Sandbox_SandboxSession_SessionId",
                table: "Senparc_Sandbox_SandboxSession",
                column: "SessionId",
                unique: true);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_Sandbox_SandboxSession");
        }
    }
}
