/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260927164001_AddWeixinClawBindingProfiles.cs
    文件功能描述：20260927164001_AddWeixinClawBindingProfiles.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddWeixinClawBindingProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeixinManager_WeixinClawBindingProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeixinClawAccountId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    WorkflowId = table.Column<int>(type: "int", nullable: true),
                    AiModelId = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    EnableNeuBell = table.Column<bool>(type: "bit", nullable: false),
                    EnableWorkflow = table.Column<bool>(type: "bit", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    BindingCodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_WeixinClawBindingProfile", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawBindingProfile_WeixinClawAccountId_BindingCodeHash",
                table: "WeixinManager_WeixinClawBindingProfile",
                columns: new[] { "WeixinClawAccountId", "BindingCodeHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawBindingProfile_WeixinClawAccountId_Enabled",
                table: "WeixinManager_WeixinClawBindingProfile",
                columns: new[] { "WeixinClawAccountId", "Enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeixinManager_WeixinClawBindingProfile");
        }
    }
}
