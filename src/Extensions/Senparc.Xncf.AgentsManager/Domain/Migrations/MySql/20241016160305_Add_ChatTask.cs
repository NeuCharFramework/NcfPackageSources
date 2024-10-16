using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Add_ChatTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~1",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~2",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.AddColumn<int>(
                name: "ChatTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MyProperty",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Avastar",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_ChatTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChatGroupId = table.Column<int>(type: "int", nullable: false),
                    AiModelId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PromptCommand = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPersonality = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Score = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResultComment = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HookPlatform = table.Column<int>(type: "int", nullable: false),
                    HookPlatformParameter = table.Column<string>(type: "longtext", nullable: true)
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
                    table.PrimaryKey("PK_Senparc_AgentsManager_ChatTask", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupHistory_ChatTaskId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ChatTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~1",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ChatTaskId",
                principalTable: "Senparc_AgentsManager_ChatTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~2",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "FromAgentTemplateId",
                principalTable: "Senparc_AgentsManager_AgentTemplate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~3",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ToAgentTemplateId",
                principalTable: "Senparc_AgentsManager_AgentTemplate",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~1",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~2",
                table: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~3",
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
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~1",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "FromAgentTemplateId",
                principalTable: "Senparc_AgentsManager_AgentTemplate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~2",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ToAgentTemplateId",
                principalTable: "Senparc_AgentsManager_AgentTemplate",
                principalColumn: "Id");
        }
    }
}
