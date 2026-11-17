/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: 20261025162801_AddFineTuningDatasetFileReference.cs
    File description: 20261025162801_AddFineTuningDatasetFileReference related definitions and implementation


    Created by: Senparc - 20210105

    Modified by: Senparc - 20261103
    Change description: v0.17.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class AddFineTuningDatasetFileReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningDataset",
                type: "INT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningDataset");
        }
    }
}
