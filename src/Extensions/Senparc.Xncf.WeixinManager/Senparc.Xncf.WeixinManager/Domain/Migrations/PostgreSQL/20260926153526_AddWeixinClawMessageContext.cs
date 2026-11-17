/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260926153526_AddWeixinClawMessageContext.cs
    File description: 20260926153526_AddWeixinClawMessageContext.cs implementation and project behavior.

    Created by: Senparc - 20250712

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class AddWeixinClawMessageContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastMessageContextTokenProtected",
                table: "WeixinManager_WeixinClawAccount",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessageFromUserId",
                table: "WeixinManager_WeixinClawAccount",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMessageContextTokenProtected",
                table: "WeixinManager_WeixinClawAccount");

            migrationBuilder.DropColumn(
                name: "LastMessageFromUserId",
                table: "WeixinManager_WeixinClawAccount");
        }
    }
}
