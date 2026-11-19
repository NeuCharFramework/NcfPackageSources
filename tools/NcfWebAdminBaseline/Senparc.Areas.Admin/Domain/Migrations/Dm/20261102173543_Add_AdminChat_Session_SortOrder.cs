/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20261102173543_Add_AdminChat_Session_SortOrder.cs
    File description: 20261102173543_Add_AdminChat_Session_SortOrder related definitions and implementation

    Created by: Senparc - 20210105

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_AdminChat_Session_SortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ADMIN_AdminChatSession",
                type: "INT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ADMIN_AdminChatSession");
        }
    }
}
