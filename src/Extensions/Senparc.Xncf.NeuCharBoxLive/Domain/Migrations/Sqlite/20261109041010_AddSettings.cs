/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261109041010_AddSettings.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Migrations）：20261109041010_AddSettings


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增设置表迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_NeuCharBoxLive_Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlanModelId = table.Column<int>(type: "INTEGER", nullable: true),
                    RunModelId = table.Column<int>(type: "INTEGER", nullable: true),
                    PerCodeDaily = table.Column<int>(type: "INTEGER", nullable: false),
                    GlobalDaily = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyTokenBudget = table.Column<long>(type: "INTEGER", nullable: false),
                    ExtraOrigins = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_NeuCharBoxLive_Settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_NeuCharBoxLive_Settings");
        }
    }
}
