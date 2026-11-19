/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20261024142509_AddAdminChatAttachments.cs
    File description: 20261024142509_AddAdminChatAttachments related definitions and implementation

    Created by: Senparc - 20210105

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddAdminChatAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentsJson",
                table: "ADMIN_AdminChatMessage",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentsJson",
                table: "ADMIN_AdminChatMessage");
        }
    }
}
