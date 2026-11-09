/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261107232402_Init.cs
    文件功能描述：SQL Server 迁移：创建邀请码、申请与运行记录三张表


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_Application",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Interest = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    WouldTry = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Scene = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CodeId = table.Column<int>(type: "int", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_Application", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_InviteCode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Normalized = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DailyLimit = table.Column<int>(type: "int", nullable: false),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    UsesTotal = table.Column<int>(type: "int", nullable: false),
                    LastUsedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_InviteCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_Run",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Scene = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhatIf = table.Column<bool>(type: "bit", nullable: false),
                    WhatIfDevice = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PlanJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Calls = table.Column<int>(type: "int", nullable: false),
                    PromptTokens = table.Column<int>(type: "int", nullable: false),
                    CompletionTokens = table.Column<int>(type: "int", nullable: false),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    FinishedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_Run", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_NeuCharBoxLive_InviteCode_Normalized",
                table: "Senparc_NeuCharBoxLive_InviteCode",
                column: "Normalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_NeuCharBoxLive_Run_Day",
                table: "Senparc_NeuCharBoxLive_Run",
                column: "Day");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_NeuCharBoxLive_Application");

            migrationBuilder.DropTable(
                name: "Senparc_NeuCharBoxLive_InviteCode");

            migrationBuilder.DropTable(
                name: "Senparc_NeuCharBoxLive_Run");
        }
    }
}
