/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: 20260703153547_Add_IsArchived.cs
    File description: database migration definition
    
    
    Created by: Senparc - 20250816
    
    Modified by: Senparc - 20260704
    Change description: v0.11.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_IsArchived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Senparc_AgentsManager_ChatTask",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Senparc_AgentsManager_ChatTask");
        }
    }
}
