/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: 20261021125808_Add_PromptRange_SkillKind.cs
    File description: 20261021125808_Add_PromptRange_SkillKind related definitions and implementation


    Created by: Senparc - 20210105

    Modified by: Senparc - 20261103
    Change description: v0.20.8 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Add_PromptRange_SkillKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Senparc_PromptRange_PromptRange",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Senparc_PromptRange_PromptRange");
        }
    }
}
