/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261109041016_AddSettings.cs
    文件功能描述：SQL Server 迁移：新增设置表（Senparc_NeuCharBoxLive_Settings）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增设置表迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Migrations.SqlServer
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    PlanModelId = table.Column<int>(type: "int", nullable: true),
                    RunModelId = table.Column<int>(type: "int", nullable: true),
                    PerCodeDaily = table.Column<int>(type: "int", nullable: false),
                    GlobalDaily = table.Column<int>(type: "int", nullable: false),
                    DailyTokenBudget = table.Column<long>(type: "bigint", nullable: false),
                    ExtraOrigins = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
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
