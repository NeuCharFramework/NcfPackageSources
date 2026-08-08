using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Sandbox.Domain.Migrations.MySql
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Senparc_Sandbox_SandboxSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),

                    SessionId = table.Column<string>(type: "varchar(1000)", maxLength: 64, nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    TemplateKey = table.Column<string>(type: "varchar(1000)", maxLength: 64, nullable: false),
                    RuntimeKind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RuntimeHandle = table.Column<string>(type: "varchar(1000)", maxLength: 128, nullable: true),
                    HostPort = table.Column<int>(type: "int", nullable: true),
                    AccessUrl = table.Column<string>(type: "varchar(1000)", maxLength: 500, nullable: true),
                    AccessToken = table.Column<string>(type: "varchar(1000)", maxLength: 128, nullable: true),
                    CpuLimit = table.Column<double>(type: "double", nullable: false),
                    MemoryMb = table.Column<int>(type: "int", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StatusMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "varchar(1000)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "varchar(1000)", maxLength: 300, nullable: true)
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
