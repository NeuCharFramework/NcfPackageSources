/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260811140000_AddExecutionReplay.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260811

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Senparc.Xncf.NeuCharWorkflow.Models;

#nullable disable

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Migrations.PostgreSQL;

[DbContext(typeof(NeuCharWorkflowSenparcEntities_PostgreSQL))]
[Migration("20260811140000_AddExecutionReplay")]
public partial class AddExecutionReplay : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ReplayEventsJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ReplaySnapshotHash", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ReplaySnapshotJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", type: "text", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ReplayEventsJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");
        migrationBuilder.DropColumn(name: "ReplaySnapshotHash", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");
        migrationBuilder.DropColumn(name: "ReplaySnapshotJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");
    }
}
