/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261021161000_Add_AgentTemplate_SkillBindings_WorkspacePolicy.cs
    文件功能描述：20261021161000_Add_AgentTemplate_SkillBindings_WorkspacePolicy 相关定义与实现


    创建标识：Senparc - 20250120

    修改标识：Senparc - 20261103
    修改描述：v0.19.1 支持 Add_AgentTemplate_SkillBindings_WorkspacePolicy 数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.MySql
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
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WorkspacePolicyJson",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
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
