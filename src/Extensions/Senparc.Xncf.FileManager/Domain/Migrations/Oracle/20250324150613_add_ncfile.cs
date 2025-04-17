using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.FileManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class add_ncfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NcfFiles",
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
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NcfFiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NcfFiles");
        }
    }
}
