using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_PromptRange_SkillArtifactSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceFileId",
                table: "Senparc_PromptRange_PromptRange",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Senparc_PromptRange_PromptRange",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Senparc_PromptRange_PromptRange",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceFileId",
                table: "Senparc_PromptRange_PromptRange");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Senparc_PromptRange_PromptRange");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Senparc_PromptRange_PromptRange");
        }
    }
}
