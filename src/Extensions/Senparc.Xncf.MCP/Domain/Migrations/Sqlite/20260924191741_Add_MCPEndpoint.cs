using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.MCP.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_MCPEndpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_MCP_MCPEndpoint",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EndpointType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ProtocolVersion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuthConfig = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ExtraConfig = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    LastTestedTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastTestResult = table.Column<bool>(type: "INTEGER", nullable: true),
                    LastToolsJson = table.Column<string>(type: "TEXT", nullable: true),
                    LastToolCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_MCP_MCPEndpoint", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_MCP_MCPEndpoint");
        }
    }
}
