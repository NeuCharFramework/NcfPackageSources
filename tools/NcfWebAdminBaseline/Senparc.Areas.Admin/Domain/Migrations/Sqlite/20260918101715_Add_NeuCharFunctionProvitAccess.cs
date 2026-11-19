/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260918101715_Add_NeuCharFunctionProvitAccess.cs
    File description: database migration and model snapshot
    Change description: v0.9.1 Updated module behavior and related support.

    Created by: Senparc - 20260918

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_NeuCharFunctionProvitAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharFunctionProvitAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModuleUid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FunctionKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AccessMode = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowedRoleCodes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AllowedPermissionCodes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AllowedUserIds = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharFunctionProvitAccess", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharFunctionProvitAccess_ModuleUid_FunctionKey",
                table: "ADMIN_NeuCharFunctionProvitAccess",
                columns: new[] { "ModuleUid", "FunctionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharFunctionProvitAccess");
        }
    }
}