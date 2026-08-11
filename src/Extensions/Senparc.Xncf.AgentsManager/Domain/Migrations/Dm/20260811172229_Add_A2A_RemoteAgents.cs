using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Dm
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
                type: "NVARCHAR2(32767)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromParticipantKind",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "NVARCHAR2(32767)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromParticipantName",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "NVARCHAR2(32767)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemoteContextId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "NVARCHAR2(32767)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemoteTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "NVARCHAR2(32767)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContextSharingMode",
                table: "Senparc_AgentsManager_ChatGroup",
                type: "INT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_RemoteAgent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Enable = table.Column<bool>(type: "BIT", nullable: false),
                    Protocol = table.Column<int>(type: "INT", nullable: false),
                    AgentCardUrl = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "INT", nullable: false),
                    AuthHeaderName = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    AuthSecretKey = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    TimeoutSeconds = table.Column<int>(type: "INT", nullable: false),
                    ConnectionStatus = table.Column<int>(type: "INT", nullable: false),
                    LastHealthCheckAt = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    LastHealthCheckMessage = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_RemoteAgent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_ChatGroupRemoteMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    UID = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    ChatGroupId = table.Column<int>(type: "INT", nullable: false),
                    RemoteAgentId = table.Column<int>(type: "INT", nullable: false),
                    Enable = table.Column<bool>(type: "BIT", nullable: false),
                    ContextSharingMode = table.Column<int>(type: "INT", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_ChatGroupRemoteMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_ChatGroupRemoteMember_Senparc_AgentsManager_RemoteAgent_RemoteAgentId",
                        column: x => x.RemoteAgentId,
                        principalTable: "Senparc_AgentsManager_RemoteAgent",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupRemoteMember_ChatGroupId",
                table: "Senparc_AgentsManager_ChatGroupRemoteMember",
                column: "ChatGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupRemoteMember_ChatGroupId_RemoteAgentId",
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
