/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260926153644_AddWeixinClawMessageContext.cs
    文件功能描述：20260926153644_AddWeixinClawMessageContext.cs implementation and project behavior.


    创建标识：Senparc - 20260927

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Dm
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
                type: "NVARCHAR2(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessageFromUserId",
                table: "WeixinManager_WeixinClawAccount",
                type: "NVARCHAR2(300)",
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
