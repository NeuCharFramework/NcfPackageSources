/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: 20260811140000_AddExecutionReplay.cs
    File description: database migration and model snapshot


    Created by: Senparc - 20260811

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Senparc.Xncf.NeuCharWorkflow.Models;

#nullable disable

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Migrations.Dm;

[DbContext(typeof(NeuCharWorkflowSenparcEntities_Dm))]
[Migration("20260811140000_AddExecutionReplay")]
public partial class AddExecutionReplay : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ReplayEventsJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", type: "CLOB", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ReplaySnapshotHash", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", type: "NVARCHAR2(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ReplaySnapshotJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", type: "CLOB", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ReplayEventsJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");
        migrationBuilder.DropColumn(name: "ReplaySnapshotHash", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");
        migrationBuilder.DropColumn(name: "ReplaySnapshotJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");
    }
}
