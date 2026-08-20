/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260817074726_Add_AgentTemplate_ModelBinding.cs
    文件功能描述：AgentTemplate 模型绑定数据库迁移
    
    
    创建标识：Senparc - 20260817
    
    修改标识：Senparc - 20260817
    修改描述：v0.16.0 补充 AgentTemplate.AiModelId 多数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Sqlite
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
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModelBinding",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "INTEGER",
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
