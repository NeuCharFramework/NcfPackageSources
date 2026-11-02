/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261004135845_Remove_DatabaseSample.cs
    文件功能描述：20261004135845_Remove_DatabaseSample 相关定义与实现


    创建标识：Senparc - 20211114

    修改标识：Senparc - 20261103
    修改描述：v0.5.10 支持 Remove_DatabaseSample 数据库迁移

----------------------------------------------------------------*/

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
