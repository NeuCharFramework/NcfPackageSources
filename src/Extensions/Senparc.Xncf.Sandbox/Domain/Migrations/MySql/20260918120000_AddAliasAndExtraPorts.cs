/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260918120000_AddAliasAndExtraPorts.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260918

    修改标识：Senparc - 20260918
    修改描述：v0.3.3 增加沙箱会话别名与附加端口映射列

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Sandbox.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddAliasAndExtraPorts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "Senparc_Sandbox_SandboxSession",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExtraPorts",
                table: "Senparc_Sandbox_SandboxSession",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraPorts",
                table: "Senparc_Sandbox_SandboxSession");

            migrationBuilder.DropColumn(
                name: "Alias",
                table: "Senparc_Sandbox_SandboxSession");
        }
    }
}
