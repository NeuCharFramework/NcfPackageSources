/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: 20260828120000_Add_NeuCharPivotLoopTask_Workflow.cs
    File description: database migration definition


    Created by: Senparc - 20260828

    Modified by: Senparc - 20260829
    Change description: v0.7.0 Updated module behavior and related support.

----------------------------------------------------------------*/

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
