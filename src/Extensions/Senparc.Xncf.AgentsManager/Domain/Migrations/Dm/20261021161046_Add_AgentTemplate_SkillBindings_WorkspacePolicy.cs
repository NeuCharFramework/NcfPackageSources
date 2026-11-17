/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20261021161046_Add_AgentTemplate_SkillBindings_WorkspacePolicy.cs
    File description: 20261021161046_Add_AgentTemplate_SkillBindings_WorkspacePolicy related definitions and implementation

    Created by: Senparc - 20250120

    Modified by: Senparc - 20261103
    Change description: v0.19.1 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_AgentTemplate_SkillBindings_WorkspacePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkillBindingsJson",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "NVARCHAR2(32767)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkspacePolicyJson",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "NVARCHAR2(32767)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkillBindingsJson",
                table: "Senparc_AgentsManager_AgentTemplate");

            migrationBuilder.DropColumn(
                name: "WorkspacePolicyJson",
                table: "Senparc_AgentsManager_AgentTemplate");
        }
    }
}
