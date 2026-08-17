using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Add_AgentTemplate_ModelBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiModelId",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModelBinding",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_AgentTemplate_AiModelId",
                table: "Senparc_AgentsManager_AgentTemplate",
                column: "AiModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Senparc_AgentsManager_AgentTemplate_AiModelId",
                table: "Senparc_AgentsManager_AgentTemplate");

            migrationBuilder.DropColumn(
                name: "AiModelId",
                table: "Senparc_AgentsManager_AgentTemplate");

            migrationBuilder.DropColumn(
                name: "ModelBinding",
                table: "Senparc_AgentsManager_AgentTemplate");
        }
    }
}
