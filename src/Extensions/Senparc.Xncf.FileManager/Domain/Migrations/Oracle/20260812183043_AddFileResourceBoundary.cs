using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.FileManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class AddFileResourceBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResourceScope",
                table: "Senparc_FileManager_NcfFolder",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "Senparc_FileManager_NcfFile",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Senparc_FileManager_NcfFile",
                type: "NVARCHAR2(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Senparc_FileManager_NcfFile",
                type: "NVARCHAR2(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResourceScope",
                table: "Senparc_FileManager_NcfFile",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_FileManager_NcfFolder_ResourceScope_ParentId",
                table: "Senparc_FileManager_NcfFolder",
                columns: new[] { "ResourceScope", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_FileManager_NcfFile_ResourceScope_FolderId",
                table: "Senparc_FileManager_NcfFile",
                columns: new[] { "ResourceScope", "FolderId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Senparc_FileManager_NcfFolder_ResourceScope_ParentId",
                table: "Senparc_FileManager_NcfFolder");

            migrationBuilder.DropIndex(
                name: "IX_Senparc_FileManager_NcfFile_ResourceScope_FolderId",
                table: "Senparc_FileManager_NcfFile");

            migrationBuilder.DropColumn(
                name: "ResourceScope",
                table: "Senparc_FileManager_NcfFolder");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "Senparc_FileManager_NcfFile");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Senparc_FileManager_NcfFile");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Senparc_FileManager_NcfFile");

            migrationBuilder.DropColumn(
                name: "ResourceScope",
                table: "Senparc_FileManager_NcfFile");
        }
    }
}
