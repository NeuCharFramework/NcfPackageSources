/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260803133035_Add_AgentTemplate_KnowledgeBaseId.cs
    File description: Entity Framework database migration implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.14.0-preview9 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_AgentTemplate_KnowledgeBaseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KnowledgeBaseId",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "NUMBER(10)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_AgentTemplate_KnowledgeBaseId",
                table: "Senparc_AgentsManager_AgentTemplate",
                column: "KnowledgeBaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Senparc_AgentsManager_AgentTemplate_KnowledgeBaseId",
                table: "Senparc_AgentsManager_AgentTemplate");

            migrationBuilder.DropColumn(
                name: "KnowledgeBaseId",
                table: "Senparc_AgentsManager_AgentTemplate");
        }
    }
}
