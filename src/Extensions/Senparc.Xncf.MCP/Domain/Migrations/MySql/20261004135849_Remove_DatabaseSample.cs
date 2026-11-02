/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261004135849_Remove_DatabaseSample.cs
    文件功能描述：20261004135849_Remove_DatabaseSample 相关定义与实现


    创建标识：Senparc - 20211211

    修改标识：Senparc - 20261103
    修改描述：v0.5.10 支持 Remove_DatabaseSample 数据库迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.MCP.Domain.Migrations.MySql
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AdditionNote = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Blue = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Green = table.Column<int>(type: "int", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Red = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_MCP_Color", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
