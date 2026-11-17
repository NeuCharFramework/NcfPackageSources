/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260808152021_Tune_NeuCharPivot_Storage.cs
    File description: database migration and model snapshot

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Tune_NeuCharPivot_Storage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharWorkflow_Enabled_NextRunAt",
                table: "ADMIN_NeuCharWorkflow",
                columns: new[] { "Enabled", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotLoopTask_Enabled_NextRunAt",
                table: "ADMIN_NeuCharPivotLoopTask",
                columns: new[] { "Enabled", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotLoopTask_FunctionId",
                table: "ADMIN_NeuCharPivotLoopTask",
                column: "FunctionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotFunction_ModuleUid",
                table: "ADMIN_NeuCharPivotFunction",
                column: "ModuleUid");

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotFunction_PivotId_FunctionKey",
                table: "ADMIN_NeuCharPivotFunction",
                columns: new[] { "PivotId", "FunctionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotConfiguration_ModuleUid",
                table: "ADMIN_NeuCharPivotConfiguration",
                column: "ModuleUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharExecutionLog_CorrelationId",
                table: "ADMIN_NeuCharExecutionLog",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharExecutionLog_SourceType_SourceId",
                table: "ADMIN_NeuCharExecutionLog",
                columns: new[] { "SourceType", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharWorkflow_Enabled_NextRunAt",
                table: "ADMIN_NeuCharWorkflow");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotLoopTask_Enabled_NextRunAt",
                table: "ADMIN_NeuCharPivotLoopTask");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotLoopTask_FunctionId",
                table: "ADMIN_NeuCharPivotLoopTask");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotFunction_ModuleUid",
                table: "ADMIN_NeuCharPivotFunction");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotFunction_PivotId_FunctionKey",
                table: "ADMIN_NeuCharPivotFunction");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotConfiguration_ModuleUid",
                table: "ADMIN_NeuCharPivotConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharExecutionLog_CorrelationId",
                table: "ADMIN_NeuCharExecutionLog");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharExecutionLog_SourceType_SourceId",
                table: "ADMIN_NeuCharExecutionLog");
        }
    }
}
