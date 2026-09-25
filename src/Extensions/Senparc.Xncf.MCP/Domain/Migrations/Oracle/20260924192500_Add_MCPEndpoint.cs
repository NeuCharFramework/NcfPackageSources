using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Senparc.Xncf.MCP.Domain.Migrations.Oracle
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    EndpointType = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    ProtocolVersion = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AuthConfig = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    ExtraConfig = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    LastTestedTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastTestResult = table.Column<bool>(type: "NUMBER(1)", nullable: true),
                    LastToolsJson = table.Column<string>(type: "CLOB", nullable: true),
                    LastToolCount = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
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
