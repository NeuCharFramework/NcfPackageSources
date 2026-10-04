/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260926173622_AddWeixinClawMessageRecords.cs
    文件功能描述：20260926173622_AddWeixinClawMessageRecords.cs implementation and project behavior.


    创建标识：Senparc - 20260927

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.PostgreSQL
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeixinClawAccountId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Seq = table.Column<long>(type: "bigint", nullable: true),
                    FromUserId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ToUserId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    MessageState = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_WeixinClawMessageRecord", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawMessageRecord_WeixinClawAccountId_C~",
                table: "WeixinManager_WeixinClawMessageRecord",
                columns: new[] { "WeixinClawAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawMessageRecord_WeixinClawAccountId_D~",
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
