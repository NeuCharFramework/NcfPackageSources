/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261106160001_Add_AdminChatSession_HasUnread.cs
    文件功能描述：Sqlite 数据库迁移 Add_AdminChatSession_HasUnread

    创建标识：Senparc - 20261107

    修改标识：Senparc - 20261108
    修改描述：v0.13.0 Sqlite 数据库迁移 Add_AdminChatSession_HasUnread

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_AdminChatSession_HasUnread : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasUnread",
                table: "ADMIN_AdminChatSession",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasUnread",
                table: "ADMIN_AdminChatSession");
        }
    }
}
