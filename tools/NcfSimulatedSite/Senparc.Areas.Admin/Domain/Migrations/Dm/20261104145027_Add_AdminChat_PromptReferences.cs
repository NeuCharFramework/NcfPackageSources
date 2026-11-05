/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261104145027_Add_AdminChat_PromptReferences.cs
    文件功能描述：AdminChat Prompt 引用数据库迁移


    创建标识：Senparc - 20250120

    修改标识：Senparc - 20261105
    修改描述：v0.12.26 增加 AdminChat 消息的 Prompt 引用字段并更新数据库迁移

----------------------------------------------------------------*/
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Dm
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
                type: "NVARCHAR2(32767)",
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
