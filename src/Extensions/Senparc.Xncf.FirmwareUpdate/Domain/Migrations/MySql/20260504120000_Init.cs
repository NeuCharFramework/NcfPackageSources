using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.FirmwareUpdate.Domain.Migrations.MySql;

public partial class Init : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "Senparc_FirmwareUpdate_FirmwareUpdateConfig",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlHelper.GetMySqlValueGenerationStrategy()),
                AutoMirrorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                UpdateIntervalHours = table.Column<int>(type: "int", nullable: false),
                LastPeriodicSyncUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                table.PrimaryKey("PK_Senparc_FirmwareUpdate_FirmwareUpdateConfig", x => x.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Senparc_FirmwareUpdate_FirmwareUpdateConfig");
    }
}
