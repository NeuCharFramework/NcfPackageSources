using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_AgentTemplate_SkillBindings_WorkspacePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkillBindingsJson",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkspacePolicyJson",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkillBindingsJson",
                table: "Senparc_AgentsManager_AgentTemplate");

            migrationBuilder.DropColumn(
                name: "WorkspacePolicyJson",
                table: "Senparc_AgentsManager_AgentTemplate");
        }
    }
}
