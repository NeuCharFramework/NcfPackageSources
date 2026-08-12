using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.PostgreSQL
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgentTemplateId = table.Column<int>(type: "integer", nullable: false),
                    PublicAgentKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Enable = table.Column<bool>(type: "boolean", nullable: false),
                    CardName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CardDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SkillId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SkillName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SkillDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AllowFunctionCalls = table.Column<bool>(type: "boolean", nullable: false),
                    MaxInputCharacters = table.Column<int>(type: "integer", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "integer", nullable: false),
                    AuthHeaderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AuthSecretKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
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
