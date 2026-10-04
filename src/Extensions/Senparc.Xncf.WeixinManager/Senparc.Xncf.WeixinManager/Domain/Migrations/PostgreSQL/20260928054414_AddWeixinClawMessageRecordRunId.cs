/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260928054414_AddWeixinClawMessageRecordRunId.cs
    文件功能描述：20260928054414_AddWeixinClawMessageRecordRunId.cs implementation and project behavior.


    创建标识：Senparc - 20250120

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.PostgreSQL
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
                type: "character varying(300)",
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
