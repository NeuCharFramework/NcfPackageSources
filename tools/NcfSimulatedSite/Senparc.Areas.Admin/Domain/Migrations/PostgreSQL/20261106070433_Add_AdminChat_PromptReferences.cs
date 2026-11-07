/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261106070433_Add_AdminChat_PromptReferences.cs
    文件功能描述：PostgreSQL 数据库迁移 Add_AdminChat_PromptReferences

    创建标识：Senparc - 20261106

    修改标识：Senparc - 20261108
    修改描述：v0.13.0 PostgreSQL 数据库迁移 Add_AdminChat_PromptReferences

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_AdminChat_PromptReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PromptReferencesJson",
                table: "ADMIN_AdminChatMessage",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromptReferencesJson",
                table: "ADMIN_AdminChatMessage");
        }
    }
}
