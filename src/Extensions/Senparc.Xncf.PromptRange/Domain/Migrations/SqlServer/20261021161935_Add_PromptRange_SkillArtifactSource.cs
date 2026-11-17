/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: 20261021161935_Add_PromptRange_SkillArtifactSource.cs
    File description: 20261021161935_Add_PromptRange_SkillArtifactSource related definitions and implementation


    Created by: Senparc - 20261022

    Modified by: Senparc - 20261103
    Change description: v0.20.8 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.SqlServer
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
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Senparc_PromptRange_PromptRange",
                type: "nvarchar(2048)",
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
