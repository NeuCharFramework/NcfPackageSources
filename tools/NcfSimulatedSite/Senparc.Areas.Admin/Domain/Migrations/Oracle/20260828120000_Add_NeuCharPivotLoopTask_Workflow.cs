using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle;

public partial class Add_NeuCharPivotLoopTask_Workflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "WorkflowId",
            table: "ADMIN_NeuCharPivotLoopTask",
            type: "NUMBER(10)",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ADMIN_NeuCharPivotLoopTask_WorkflowId",
            table: "ADMIN_NeuCharPivotLoopTask",
            column: "WorkflowId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ADMIN_NeuCharPivotLoopTask_WorkflowId",
            table: "ADMIN_NeuCharPivotLoopTask");

        migrationBuilder.DropColumn(
            name: "WorkflowId",
            table: "ADMIN_NeuCharPivotLoopTask");
    }
}
