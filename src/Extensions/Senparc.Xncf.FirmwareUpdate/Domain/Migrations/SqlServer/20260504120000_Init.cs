using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.FirmwareUpdate.Domain.Migrations.SqlServer;

public partial class Init : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Senparc_FirmwareUpdate_FirmwareUpdateConfig",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AutoMirrorEnabled = table.Column<bool>(type: "bit", nullable: false),
                UpdateIntervalHours = table.Column<int>(type: "int", nullable: false),
                LastPeriodicSyncUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                Flag = table.Column<bool>(type: "bit", nullable: false),
                AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                TenantId = table.Column<int>(type: "int", nullable: false),
                AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
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
