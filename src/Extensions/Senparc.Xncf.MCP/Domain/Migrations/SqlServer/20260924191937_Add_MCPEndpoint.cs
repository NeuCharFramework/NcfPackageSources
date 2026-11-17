/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260924191937_Add_MCPEndpoint.cs
    File description: 20260924191937_Add_MCPEndpoint.cs implementation and project behavior.

    Created by: Senparc - 20260925

    Modified by: Senparc - 20261005
    Change description: v0.5.6 0.5.6 feat: add MCP endpoint selection feature in AgentsManager

    Modified by: Senparc - 20261103
    Change description: v0.5.10 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.MCP.Domain.Migrations.SqlServer
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EndpointType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProtocolVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    AuthConfig = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExtraConfig = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastTestedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTestResult = table.Column<bool>(type: "bit", nullable: true),
                    LastToolsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastToolCount = table.Column<int>(type: "int", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
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
