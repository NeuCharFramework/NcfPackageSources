/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260911075800_Add_NeuBellWebHookLog.cs
    File description: database migration and model snapshot
    Change description: v0.7.1 Updated module behavior and related support.

    Created by: Senparc - 20260911

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Add_NeuBellWebHookLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_NeuBellWebHookLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EventKind = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WebHookUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Payload = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Result = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    ElapsedMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    FinishTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuBellWebHookLog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuBellWebHookLog_AddTime",
                table: "ADMIN_NeuBellWebHookLog",
                column: "AddTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuBellWebHookLog");
        }
    }
}
