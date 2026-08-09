using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NEUCHAR_WORKFLOW_NeuCharWorkflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    GraphJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    AdminUserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Enabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    TriggerType = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: false),
                    TriggerConfigJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastSucceeded = table.Column<bool>(type: "NUMBER(1)", nullable: true),
                    LastError = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Revision = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AutoSaveMinutes = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    LegacySourceKey = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NEUCHAR_WORKFLOW_NeuCharWorkflow", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkflowId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    WorkflowName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    Succeeded = table.Column<bool>(type: "NUMBER(1)", nullable: true),
                    ResultSummary = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Error = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NEUCHAR_WORKFLOW_NeuCharWorkflowVersion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkflowId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Revision = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    GraphJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Enabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    TriggerType = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: false),
                    TriggerConfigJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    AutoSaveMinutes = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminUserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    SaveSource = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    LegacySourceKey = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NEUCHAR_WORKFLOW_NeuCharWorkflowVersion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NEUCHAR_WORKFLOW_NeuCharWorkflow_LegacySourceKey",
                table: "NEUCHAR_WORKFLOW_NeuCharWorkflow",
                column: "LegacySourceKey",
                unique: true,
                filter: "\"LegacySourceKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog_WorkflowId_StartedAt",
                table: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog",
                columns: new[] { "WorkflowId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NEUCHAR_WORKFLOW_NeuCharWorkflowVersion_LegacySourceKey",
                table: "NEUCHAR_WORKFLOW_NeuCharWorkflowVersion",
                column: "LegacySourceKey",
                unique: true,
                filter: "\"LegacySourceKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NEUCHAR_WORKFLOW_NeuCharWorkflowVersion_WorkflowId_Revision",
                table: "NEUCHAR_WORKFLOW_NeuCharWorkflowVersion",
                columns: new[] { "WorkflowId", "Revision" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NEUCHAR_WORKFLOW_NeuCharWorkflow");

            migrationBuilder.DropTable(
                name: "NEUCHAR_WORKFLOW_NeuCharWorkflowExecutionLog");

            migrationBuilder.DropTable(
                name: "NEUCHAR_WORKFLOW_NeuCharWorkflowVersion");
        }
    }
}
