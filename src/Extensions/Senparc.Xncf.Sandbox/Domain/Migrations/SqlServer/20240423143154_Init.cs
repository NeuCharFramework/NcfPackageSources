using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Sandbox.Domain.Migrations.SqlServer
{
    /// <summary>
    /// 初始建表。注意：SQL Server 索引键不能使用 nvarchar(max)，字符串列必须带明确长度。
    /// </summary>
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_Sandbox_SandboxSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RuntimeKind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RuntimeHandle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    HostPort = table.Column<int>(type: "int", nullable: true),
                    AccessUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccessToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CpuLimit = table.Column<double>(type: "float", nullable: false),
                    MemoryMb = table.Column<int>(type: "int", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
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
