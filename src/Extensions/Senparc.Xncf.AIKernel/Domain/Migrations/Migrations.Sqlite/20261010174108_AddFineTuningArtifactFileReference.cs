using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.Sqlite
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
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "TEXT",
                maxLength: 512,
                nullable: true);
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
