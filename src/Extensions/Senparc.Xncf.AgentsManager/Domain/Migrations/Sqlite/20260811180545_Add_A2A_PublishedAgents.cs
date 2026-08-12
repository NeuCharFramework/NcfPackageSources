using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_A2A_PublishedAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_PublishedA2AAgent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AgentTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    PublicAgentKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Enable = table.Column<bool>(type: "INTEGER", nullable: false),
                    CardName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CardDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SkillId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SkillName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    SkillDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AllowFunctionCalls = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxInputCharacters = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthHeaderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AuthSecretKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_PublishedA2AAgent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_PublishedA2AAgent_AgentTemplateId",
                table: "Senparc_AgentsManager_PublishedA2AAgent",
                column: "AgentTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_PublishedA2AAgent_Enable",
                table: "Senparc_AgentsManager_PublishedA2AAgent",
                column: "Enable");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_PublishedA2AAgent_PublicAgentKey",
                table: "Senparc_AgentsManager_PublishedA2AAgent",
                column: "PublicAgentKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_PublishedA2AAgent");
        }
    }
}
