/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260816155153_Add_ChatTask_HumanInTheLoopPolicy.cs
    文件功能描述：20260816155153_Add_ChatTask_HumanInTheLoopPolicy.cs 功能实现
    
    
    创建标识：Senparc - 20260817
    
    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略
-

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 补充 ChatTask HIL 策略多数据库迁移

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 增强 Agent 工作流校验、函数绑定与任务管理交互

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.SqlServer
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
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ExecutionPolicyCaptured",
                table: "Senparc_AgentsManager_ChatTask",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HumanInTheLoopLevel",
                table: "Senparc_AgentsManager_ChatTask",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeHumanParticipant",
                table: "Senparc_AgentsManager_ChatTask",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "McpToolPermission",
                table: "Senparc_AgentsManager_ChatTask",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PluginToolPermission",
                table: "Senparc_AgentsManager_ChatTask",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireHumanApproval",
                table: "Senparc_AgentsManager_ChatTask",
                type: "bit",
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
