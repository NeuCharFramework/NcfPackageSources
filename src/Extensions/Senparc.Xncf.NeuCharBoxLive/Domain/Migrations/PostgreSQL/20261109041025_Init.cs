/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261109041025_Init.cs
    文件功能描述：PostgreSQL 迁移：创建邀请码、申请与运行记录三张表


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增初始迁移（含邀请码、申请、运行记录与设置表）

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Migrations.PostgreSQL
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Interest = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    WouldTry = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Scene = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CodeId = table.Column<int>(type: "integer", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_Application", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_InviteCode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Normalized = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DailyLimit = table.Column<int>(type: "integer", nullable: false),
                    Disabled = table.Column<bool>(type: "boolean", nullable: false),
                    UsesTotal = table.Column<int>(type: "integer", nullable: false),
                    LastUsedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_InviteCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_Run",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Scene = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    WhatIf = table.Column<bool>(type: "boolean", nullable: false),
                    WhatIfDevice = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PlanJson = table.Column<string>(type: "text", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    Calls = table.Column<int>(type: "integer", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    FinishedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_Run", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    PlanModelId = table.Column<int>(type: "integer", nullable: true),
                    RunModelId = table.Column<int>(type: "integer", nullable: true),
                    PerCodeDaily = table.Column<int>(type: "integer", nullable: false),
                    GlobalDaily = table.Column<int>(type: "integer", nullable: false),
                    DailyTokenBudget = table.Column<long>(type: "bigint", nullable: false),
                    ExtraOrigins = table.Column<string>(type: "text", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_Settings", x => x.Id);
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

            migrationBuilder.DropTable(
                name: "Senparc_NeuCharBoxLive_Settings");
        }
    }
}
