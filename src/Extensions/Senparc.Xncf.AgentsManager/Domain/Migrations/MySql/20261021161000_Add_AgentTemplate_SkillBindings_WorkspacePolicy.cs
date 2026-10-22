using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.MySql
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
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WorkspacePolicyJson",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
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
