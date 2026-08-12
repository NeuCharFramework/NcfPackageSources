/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260811172220_Add_A2A_RemoteAgents.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260812

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.PostgreSQL
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
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromParticipantKind",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromParticipantName",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemoteContextId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemoteTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContextSharingMode",
                table: "Senparc_AgentsManager_ChatGroup",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_RemoteAgent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Enable = table.Column<bool>(type: "boolean", nullable: false),
                    Protocol = table.Column<int>(type: "integer", nullable: false),
                    AgentCardUrl = table.Column<string>(type: "text", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "integer", nullable: false),
                    AuthHeaderName = table.Column<string>(type: "text", nullable: true),
                    AuthSecretKey = table.Column<string>(type: "text", nullable: true),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    ConnectionStatus = table.Column<int>(type: "integer", nullable: false),
                    LastHealthCheckAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastHealthCheckMessage = table.Column<string>(type: "text", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_RemoteAgent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_ChatGroupRemoteMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UID = table.Column<string>(type: "text", nullable: false),
                    ChatGroupId = table.Column<int>(type: "integer", nullable: false),
                    RemoteAgentId = table.Column<int>(type: "integer", nullable: false),
                    Enable = table.Column<bool>(type: "boolean", nullable: false),
                    ContextSharingMode = table.Column<int>(type: "integer", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_ChatGroupRemoteMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_ChatGroupRemoteMember_Senparc_AgentsM~",
                        column: x => x.RemoteAgentId,
                        principalTable: "Senparc_AgentsManager_RemoteAgent",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupRemoteMember_ChatGroupId",
                table: "Senparc_AgentsManager_ChatGroupRemoteMember",
                column: "ChatGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupRemoteMember_ChatGroupId_Rem~",
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
