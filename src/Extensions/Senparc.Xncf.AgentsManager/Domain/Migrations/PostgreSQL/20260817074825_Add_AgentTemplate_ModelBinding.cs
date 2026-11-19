/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260817074825_Add_AgentTemplate_ModelBinding.cs
    File description: AgentTemplate model-binding database migration

    Created by: Senparc - 20260817

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
    public partial class Add_AgentTemplate_ModelBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiModelId",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModelBinding",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_AgentTemplate_AiModelId",
                table: "Senparc_AgentsManager_AgentTemplate",
                column: "AiModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Senparc_AgentsManager_AgentTemplate_AiModelId",
                table: "Senparc_AgentsManager_AgentTemplate");

            migrationBuilder.DropColumn(
                name: "AiModelId",
                table: "Senparc_AgentsManager_AgentTemplate");

            migrationBuilder.DropColumn(
                name: "ModelBinding",
                table: "Senparc_AgentsManager_AgentTemplate");
        }
    }
}
