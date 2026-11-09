using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Migrations.Dm
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
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Email = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: true),
                    Interest = table.Column<string>(type: "NVARCHAR2(16)", maxLength: 16, nullable: false),
                    WouldTry = table.Column<string>(type: "NVARCHAR2(400)", maxLength: 400, nullable: false),
                    Scene = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(16)", maxLength: 16, nullable: false),
                    CodeId = table.Column<int>(type: "INT", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_Application", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_InviteCode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Code = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: false),
                    Normalized = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: false),
                    Label = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    DailyLimit = table.Column<int>(type: "INT", nullable: false),
                    Disabled = table.Column<bool>(type: "BIT", nullable: false),
                    UsesTotal = table.Column<int>(type: "INT", nullable: false),
                    LastUsedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_InviteCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_Run",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Code = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: false),
                    Scene = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: false),
                    Text = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    WhatIf = table.Column<bool>(type: "BIT", nullable: false),
                    WhatIfDevice = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    Model = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Day = table.Column<string>(type: "NVARCHAR2(10)", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    Outcome = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: true),
                    PlanJson = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Summary = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Calls = table.Column<int>(type: "INT", nullable: false),
                    PromptTokens = table.Column<int>(type: "INT", nullable: false),
                    CompletionTokens = table.Column<int>(type: "INT", nullable: false),
                    LatencyMs = table.Column<long>(type: "BIGINT", nullable: false),
                    FinishedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_Run", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Enabled = table.Column<bool>(type: "BIT", nullable: false),
                    PlanModelId = table.Column<int>(type: "INT", nullable: true),
                    RunModelId = table.Column<int>(type: "INT", nullable: true),
                    PerCodeDaily = table.Column<int>(type: "INT", nullable: false),
                    GlobalDaily = table.Column<int>(type: "INT", nullable: false),
                    DailyTokenBudget = table.Column<long>(type: "BIGINT", nullable: false),
                    ExtraOrigins = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
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
