using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddFineTuningArtifactFileReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentSha256",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentSha256",
                table: "Senparc_AIKernel_AIFineTuningArtifact");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningArtifact");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "Senparc_AIKernel_AIFineTuningArtifact");
        }
    }
}
