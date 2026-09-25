using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.MCP.Domain.Migrations.PostgreSQL
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EndpointType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProtocolVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    AuthConfig = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExtraConfig = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastTestedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastTestResult = table.Column<bool>(type: "boolean", nullable: true),
                    LastToolsJson = table.Column<string>(type: "text", nullable: true),
                    LastToolCount = table.Column<int>(type: "integer", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
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
