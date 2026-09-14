/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260913221239_AddChatMessages.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260913

    修改标识：Senparc - 20260913
    修改描述：v0.4.0 新增 Chat 消息持久化表

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Migrations.PostgreSQL
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowId = table.Column<int>(type: "integer", nullable: false),
                    ParticipantKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage_WorkflowId_Part~",
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
