using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.MySql
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
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Senparc_PromptRange_PromptRange",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Senparc_PromptRange_PromptRange",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
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
