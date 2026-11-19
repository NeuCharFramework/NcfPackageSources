/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20261010174123_AddFineTuningArtifactFileReference.cs
    File description: EF Core database migration

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.PostgreSQL
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
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "character varying(512)",
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
