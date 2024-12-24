using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_ChatTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MyProperty",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Avastar",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_ChatTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ChatGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    AiModelId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PromptCommand = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsPersonality = table.Column<bool>(type: "INTEGER", nullable: false),
                    Score = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResultComment = table.Column<string>(type: "TEXT", nullable: true),
                    HookPlatform = table.Column<int>(type: "INTEGER", nullable: false),
                    HookPlatformParameter = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManager_ChatTask_ChatTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ChatTaskId",
                principalTable: "Senparc_AgentsManager_ChatTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManager_ChatTask_ChatTaskId",
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
        }
    }
}
