﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.SystemManager.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedBacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "INT", nullable: false),
                    Content = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedBacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    SystemName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    MchId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    MchKey = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    TenPayAppId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    HideModuleManager = table.Column<bool>(type: "BIT", nullable: true),
                    NeuCharDeveloperId = table.Column<int>(type: "INT", nullable: false),
                    NeuCharAppKey = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    NeuCharAppSecret = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedBacks");

            migrationBuilder.DropTable(
                name: "SystemConfigs");
        }
    }
}
