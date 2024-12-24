using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_ChatTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~1",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~2",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.AddColumn<int>(
                name: "ChatTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MyProperty",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Avastar",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_ChatTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ChatGroupId = table.Column<int>(type: "integer", nullable: false),
                    AiModelId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PromptCommand = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsPersonality = table.Column<bool>(type: "boolean", nullable: false),
                    Score = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ResultComment = table.Column<string>(type: "text", nullable: true),
                    HookPlatform = table.Column<int>(type: "integer", nullable: false),
                    HookPlatformParameter = table.Column<string>(type: "text", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_ChatTask", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupHistory_ChatTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ChatTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~1",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ChatTaskId",
                principalTable: "Senparc_AgentsManager_ChatTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~2",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "FromAgentTemplateId",
                principalTable: "Senparc_AgentsManager_AgentTemplate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~3",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ToAgentTemplateId",
                principalTable: "Senparc_AgentsManager_AgentTemplate",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~1",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~2",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~3",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_ChatTask");

            migrationBuilder.DropIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupHistory_ChatTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropColumn(
                name: "ChatTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropColumn(
                name: "MyProperty",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropColumn(
                name: "Avastar",
                table: "Senparc_AgentsManager_AgentTemplate");

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~1",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "FromAgentTemplateId",
                principalTable: "Senparc_AgentsManager_AgentTemplate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManag~2",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ToAgentTemplateId",
                principalTable: "Senparc_AgentsManager_AgentTemplate",
                principalColumn: "Id");
        }
    }
}
