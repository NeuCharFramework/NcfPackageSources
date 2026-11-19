/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260918120000_AddAliasAndExtraPorts.cs
    File description: database migration and model snapshot

    Created by: Senparc - 20260918

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.4.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Sandbox.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class AddAliasAndExtraPorts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "Senparc_Sandbox_SandboxSession",
                type: "text",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtraPorts",
                table: "Senparc_Sandbox_SandboxSession",
                type: "text",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraPorts",
                table: "Senparc_Sandbox_SandboxSession");

            migrationBuilder.DropColumn(
                name: "Alias",
                table: "Senparc_Sandbox_SandboxSession");
        }
    }
}
