using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.MCP.Domain.Migrations.Oracle
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AdditionNote = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Blue = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Green = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Red = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_MCP_Color", x => x.Id);
                });
        }
    }
}
