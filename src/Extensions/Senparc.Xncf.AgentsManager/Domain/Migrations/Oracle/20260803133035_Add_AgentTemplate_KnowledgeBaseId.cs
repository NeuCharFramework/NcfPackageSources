/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260803133035_Add_AgentTemplate_KnowledgeBaseId.cs
    文件功能描述：实现 Entity Framework 数据库迁移。


    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.14.0-preview9 新增 Agent 模板知识库关联与管理统计

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
