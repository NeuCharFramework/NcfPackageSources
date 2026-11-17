/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: 20261106070548_Add_AdminChat_PromptReferences.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20250120
    
    Modified by: Senparc - 20261110
    Change description: v0.12.27 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle
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
                type: "NCLOB",
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
