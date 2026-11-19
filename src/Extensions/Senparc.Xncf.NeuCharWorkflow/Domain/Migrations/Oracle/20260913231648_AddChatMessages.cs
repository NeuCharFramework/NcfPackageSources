/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260913231648_AddChatMessages.cs
    File description: database migration and model snapshot

    Created by: Senparc - 20260913

    Modified by: Senparc - 20260913
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.4.0 enhance Chat trigger message persistence and recovery

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class AddChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkflowId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ParticipantKeyHash = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "NVARCHAR2(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "NCLOB", nullable: false),
                    RunId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage_WorkflowId_ParticipantKeyHash",
                table: "NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage",
                columns: new[] { "WorkflowId", "ParticipantKeyHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage");
        }
    }
}
