/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260811172216_Add_A2A_RemoteAgents.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260812

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Add_A2A_RemoteAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromParticipantKey",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FromParticipantKind",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FromParticipantName",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RemoteContextId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RemoteTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ContextSharingMode",
                table: "Senparc_AgentsManager_ChatGroup",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_RemoteAgent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Enable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Protocol = table.Column<int>(type: "int", nullable: false),
                    AgentCardUrl = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthenticationMode = table.Column<int>(type: "int", nullable: false),
                    AuthHeaderName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthSecretKey = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    ConnectionStatus = table.Column<int>(type: "int", nullable: false),
                    LastHealthCheckAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastHealthCheckMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_Senparc_AgentsManager_RemoteAgent", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_ChatGroupRemoteMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChatGroupId = table.Column<int>(type: "int", nullable: false),
                    RemoteAgentId = table.Column<int>(type: "int", nullable: false),
                    Enable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ContextSharingMode = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Senparc_AgentsManager_ChatGroupRemoteMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_ChatGroupRemoteMember_Senparc_AgentsMa~",
                        column: x => x.RemoteAgentId,
                        principalTable: "Senparc_AgentsManager_RemoteAgent",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupRemoteMember_ChatGroupId",
                table: "Senparc_AgentsManager_ChatGroupRemoteMember",
                column: "ChatGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupRemoteMember_ChatGroupId_Remo~",
                table: "Senparc_AgentsManager_ChatGroupRemoteMember",
                columns: new[] { "ChatGroupId", "RemoteAgentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupRemoteMember_RemoteAgentId",
                table: "Senparc_AgentsManager_ChatGroupRemoteMember",
                column: "RemoteAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_RemoteAgent_AgentCardUrl",
                table: "Senparc_AgentsManager_RemoteAgent",
                column: "AgentCardUrl");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_RemoteAgent_Enable",
                table: "Senparc_AgentsManager_RemoteAgent",
                column: "Enable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_ChatGroupRemoteMember");

            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_RemoteAgent");

            migrationBuilder.DropColumn(
                name: "FromParticipantKey",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropColumn(
                name: "FromParticipantKind",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropColumn(
                name: "FromParticipantName",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropColumn(
                name: "RemoteContextId",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropColumn(
                name: "RemoteTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropColumn(
                name: "ContextSharingMode",
                table: "Senparc_AgentsManager_ChatGroup");
        }
    }
}
