/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260928054543_AddWeixinClawMessageRecordRunId.cs
    File description: 20260928054543_AddWeixinClawMessageRecordRunId.cs implementation and project behavior.

    Created by: Senparc - 20210105

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class AddWeixinClawMessageRecordRunId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RunId",
                table: "WeixinManager_WeixinClawMessageRecord",
                type: "NVARCHAR2(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RunId",
                table: "WeixinManager_WeixinClawMessageRecord");
        }
    }
}
