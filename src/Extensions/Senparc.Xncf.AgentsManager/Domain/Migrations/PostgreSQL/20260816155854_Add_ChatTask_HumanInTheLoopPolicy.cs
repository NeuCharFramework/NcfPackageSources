/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: 20260816155854_Add_ChatTask_HumanInTheLoopPolicy.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20260817
    
    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.
-

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_ChatTask_HumanInTheLoopPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatMaxRound",
                table: "Senparc_AgentsManager_ChatTask",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ExecutionPolicyCaptured",
                table: "Senparc_AgentsManager_ChatTask",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HumanInTheLoopLevel",
                table: "Senparc_AgentsManager_ChatTask",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeHumanParticipant",
                table: "Senparc_AgentsManager_ChatTask",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "McpToolPermission",
                table: "Senparc_AgentsManager_ChatTask",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PluginToolPermission",
                table: "Senparc_AgentsManager_ChatTask",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireHumanApproval",
                table: "Senparc_AgentsManager_ChatTask",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatMaxRound",
                table: "Senparc_AgentsManager_ChatTask");

            migrationBuilder.DropColumn(
                name: "ExecutionPolicyCaptured",
                table: "Senparc_AgentsManager_ChatTask");

            migrationBuilder.DropColumn(
                name: "HumanInTheLoopLevel",
                table: "Senparc_AgentsManager_ChatTask");

            migrationBuilder.DropColumn(
                name: "IncludeHumanParticipant",
                table: "Senparc_AgentsManager_ChatTask");

            migrationBuilder.DropColumn(
                name: "McpToolPermission",
                table: "Senparc_AgentsManager_ChatTask");

            migrationBuilder.DropColumn(
                name: "PluginToolPermission",
                table: "Senparc_AgentsManager_ChatTask");

            migrationBuilder.DropColumn(
                name: "RequireHumanApproval",
                table: "Senparc_AgentsManager_ChatTask");
        }
    }
}
