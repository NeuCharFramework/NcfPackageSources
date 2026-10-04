/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260926173647_AddWeixinClawMessageRecords.cs
    文件功能描述：20260926173647_AddWeixinClawMessageRecords.cs implementation and project behavior.


    创建标识：Senparc - 20260927

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class AddWeixinClawMessageRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeixinManager_WeixinClawMessageRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WeixinClawAccountId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Direction = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    MessageId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    Seq = table.Column<long>(type: "NUMBER(19)", nullable: true),
                    FromUserId = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    ToUserId = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    ClientId = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    MessageType = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MessageState = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Error = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    Text = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_WeixinClawMessageRecord", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawMessageRecord_WeixinClawAccountId_CreatedAt",
                table: "WeixinManager_WeixinClawMessageRecord",
                columns: new[] { "WeixinClawAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawMessageRecord_WeixinClawAccountId_Direction_MessageId",
                table: "WeixinManager_WeixinClawMessageRecord",
                columns: new[] { "WeixinClawAccountId", "Direction", "MessageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeixinManager_WeixinClawMessageRecord");
        }
    }
}
