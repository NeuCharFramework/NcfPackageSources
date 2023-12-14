using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    public partial class _20231214_Modify_VersionRule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Version",
                table: "Senparc_PromptRange_PromptItem",
                newName: "FullVersion");

            migrationBuilder.RenameColumn(
                name: "Show",
                table: "Senparc_PromptRange_PromptItem",
                newName: "IsShare");

            migrationBuilder.AddColumn<int>(
                name: "Aiming",
                table: "Senparc_PromptRange_PromptItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentTac",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tactic",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aiming",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropColumn(
                name: "ParentTac",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropColumn(
                name: "Tactic",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.RenameColumn(
                name: "IsShare",
                table: "Senparc_PromptRange_PromptItem",
                newName: "Show");

            migrationBuilder.RenameColumn(
                name: "FullVersion",
                table: "Senparc_PromptRange_PromptItem",
                newName: "Version");
        }
    }
}
