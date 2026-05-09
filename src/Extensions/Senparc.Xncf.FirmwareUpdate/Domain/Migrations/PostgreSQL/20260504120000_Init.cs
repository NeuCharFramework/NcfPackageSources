using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.FirmwareUpdate.Domain.Migrations.PostgreSQL;

public partial class Init : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Senparc_FirmwareUpdate_FirmwareUpdateConfig",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                AutoMirrorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                UpdateIntervalHours = table.Column<int>(type: "integer", nullable: false),
                LastPeriodicSyncUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                Flag = table.Column<bool>(type: "boolean", nullable: false),
                AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                TenantId = table.Column<int>(type: "integer", nullable: false),
                AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
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
