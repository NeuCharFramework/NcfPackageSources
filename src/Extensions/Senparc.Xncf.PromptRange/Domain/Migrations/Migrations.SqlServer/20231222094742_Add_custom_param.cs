using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_custom_param : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NickName",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariableDictJson",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropColumn(
                name: "Suffix",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropColumn(
                name: "VariableDictJson",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.AlterColumn<string>(
                name: "NickName",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
