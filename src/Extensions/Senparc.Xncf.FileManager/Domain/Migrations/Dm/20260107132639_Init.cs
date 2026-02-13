using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.FileManager.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_FileManager_Color",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Red = table.Column<int>(type: "INT", nullable: false),
                    Green = table.Column<int>(type: "INT", nullable: false),
                    Blue = table.Column<int>(type: "INT", nullable: false),
                    AdditionNote = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_FileManager_Color", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_FileManager_NcfFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "NVARCHAR2(250)", maxLength: 250, nullable: false),
                    StorageFileName = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    FilePath = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    FileSize = table.Column<long>(type: "BIGINT", nullable: false),
                    FileExtension = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    FileType = table.Column<int>(type: "INT", nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    UploadTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    FolderId = table.Column<int>(type: "INT", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_FileManager_NcfFile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_FileManager_NcfFolder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<int>(type: "INT", nullable: true),
                    Description = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_FileManager_NcfFolder", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_FileManager_Color");

            migrationBuilder.DropTable(
                name: "Senparc_FileManager_NcfFile");

            migrationBuilder.DropTable(
                name: "Senparc_FileManager_NcfFolder");
        }
    }
}
