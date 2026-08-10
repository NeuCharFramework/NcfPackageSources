using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Senparc.Xncf.NeuCharWorkflow.Models;

#nullable disable

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Migrations.MySql;

[DbContext(typeof(NeuCharWorkflowSenparcEntities_MySql))]
[Migration("20260811140000_AddExecutionReplay")]
public partial class AddExecutionReplay : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ReplayEventsJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", type: "longtext", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ReplaySnapshotHash", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", type: "varchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ReplaySnapshotJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", type: "longtext", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ReplayEventsJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");
        migrationBuilder.DropColumn(name: "ReplaySnapshotHash", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");
        migrationBuilder.DropColumn(name: "ReplaySnapshotJson", table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");
    }
}
