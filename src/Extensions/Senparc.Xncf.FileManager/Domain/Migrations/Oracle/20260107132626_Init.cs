using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.FileManager.Domain.Migrations.Oracle
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Red = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Green = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Blue = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdditionNote = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    FileName = table.Column<string>(type: "NVARCHAR2(250)", maxLength: 250, nullable: false),
                    StorageFileName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    FilePath = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    FileSize = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    FileExtension = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    FileType = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    UploadTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    FolderId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Description = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
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
