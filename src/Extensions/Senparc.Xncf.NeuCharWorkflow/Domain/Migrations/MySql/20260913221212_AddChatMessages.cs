/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260913221212_AddChatMessages.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260913

    修改标识：Senparc - 20260913
    修改描述：v0.4.0 新增 Chat 消息持久化表

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Migrations.MySql
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    ParticipantKeyHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RunId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage_WorkflowId_Parti~",
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
