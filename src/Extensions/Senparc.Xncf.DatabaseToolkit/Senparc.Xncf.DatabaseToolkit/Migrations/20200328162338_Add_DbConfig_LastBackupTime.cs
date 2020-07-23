using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.DatabaseToolkit.Migrations
{
    public partial class Add_DbConfig_LastBackupTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackuPath",
                table: "DatabaseToolkitDbConfig");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "DatabaseToolkitDbConfig",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "DatabaseToolkitDbConfig",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "BackupPath",
                table: "DatabaseToolkitDbConfig",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBackupTime",
                table: "DatabaseToolkitDbConfig",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupPath",
                table: "DatabaseToolkitDbConfig");

            migrationBuilder.DropColumn(
                name: "LastBackupTime",
                table: "DatabaseToolkitDbConfig");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "DatabaseToolkitDbConfig",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "DatabaseToolkitDbConfig",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AddColumn<string>(
                name: "BackuPath",
                table: "DatabaseToolkitDbConfig",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }
    }
}
