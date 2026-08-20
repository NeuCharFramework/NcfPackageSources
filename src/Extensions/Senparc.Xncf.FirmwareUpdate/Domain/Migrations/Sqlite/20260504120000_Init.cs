using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.FirmwareUpdate.Domain.Migrations.Sqlite;

public partial class Init : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Senparc_FirmwareUpdate_FirmwareUpdateConfig",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AutoMirrorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                UpdateIntervalHours = table.Column<int>(type: "INTEGER", nullable: false),
                LastPeriodicSyncUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Senparc_FirmwareUpdate_FirmwareUpdateConfig", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Senparc_FirmwareUpdate_FirmwareUpdateConfig");
    }
}
