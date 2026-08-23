/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260822164523_Add_AgentExecutionTasks.cs
    文件功能描述：20260822164523_Add_AgentExecutionTasks.cs 相关实现

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 新增独立 Agent 执行任务多数据库迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_AgentExecutionTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_AgentExecutionTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgentTemplateId = table.Column<int>(type: "integer", nullable: false),
                    AgentTemplateName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WorkflowId = table.Column<int>(type: "integer", nullable: true),
                    AdminUserId = table.Column<int>(type: "integer", nullable: false),
                    AiModelId = table.Column<int>(type: "integer", nullable: true),
                    ModelDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PromptCommand = table.Column<string>(type: "text", nullable: false),
                    Output = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    EventsJson = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AllowFunctionCalls = table.Column<bool>(type: "boolean", nullable: false),
                    HumanInTheLoopLevel = table.Column<int>(type: "integer", nullable: false),
                    PluginToolPermission = table.Column<int>(type: "integer", nullable: false),
                    McpToolPermission = table.Column<int>(type: "integer", nullable: false),
                    IsPersonality = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TotalPromptTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalCompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    ToolCallCount = table.Column<int>(type: "integer", nullable: false),
                    ResponseCount = table.Column<int>(type: "integer", nullable: false),
                    TotalResponseMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    MaxResponseMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_AgentExecutionTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_AgentExecutionTask_Senparc_AgentsMana~",
                        column: x => x.AgentTemplateId,
                        principalTable: "Senparc_AgentsManager_AgentTemplate",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_AgentExecutionTask_AgentTemplateId_St~",
                table: "Senparc_AgentsManager_AgentExecutionTask",
                columns: new[] { "AgentTemplateId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_AgentExecutionTask_CorrelationId",
                table: "Senparc_AgentsManager_AgentExecutionTask",
                column: "CorrelationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_AgentExecutionTask");
        }
    }
}
