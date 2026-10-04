using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.MCP.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Remove_DatabaseSample : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_MCP_Color");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_MCP_Color",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AdditionNote = table.Column<string>(type: "text", nullable: true),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Blue = table.Column<int>(type: "integer", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    Green = table.Column<int>(type: "integer", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Red = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TenantId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_MCP_Color", x => x.Id);
                });
        }
    }
}
