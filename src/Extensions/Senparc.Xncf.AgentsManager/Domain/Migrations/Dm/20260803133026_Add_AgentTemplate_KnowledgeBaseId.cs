using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_AgentTemplate_KnowledgeBaseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KnowledgeBaseId",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "INT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_AgentTemplate_KnowledgeBaseId",
                table: "Senparc_AgentsManager_AgentTemplate",
                column: "KnowledgeBaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Senparc_AgentsManager_AgentTemplate_KnowledgeBaseId",
                table: "Senparc_AgentsManager_AgentTemplate");

            migrationBuilder.DropColumn(
                name: "KnowledgeBaseId",
                table: "Senparc_AgentsManager_AgentTemplate");
        }
    }
}
