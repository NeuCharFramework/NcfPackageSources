/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260913220801_AddChatMessages.cs
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

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Migrations.Sqlite
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParticipantKeyHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
