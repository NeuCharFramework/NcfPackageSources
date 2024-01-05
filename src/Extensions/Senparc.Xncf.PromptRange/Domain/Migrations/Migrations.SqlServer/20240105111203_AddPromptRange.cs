using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddPromptRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatSystemPrompt",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropColumn(
                name: "ExpectedResultsJson",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropColumn(
                name: "TokenSelectionBiases",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.RenameColumn(
                name: "NumsOfResults",
                table: "Senparc_PromptRange_PromptItem",
                newName: "RangeId");

            migrationBuilder.AlterColumn<string>(
                name: "PromptItemVersion",
                table: "Senparc_PromptRange_PromptResult",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "Senparc_PromptRange_PromptResult");

            migrationBuilder.RenameColumn(
                name: "RangeId",
                table: "Senparc_PromptRange_PromptItem",
                newName: "NumsOfResults");

            migrationBuilder.AlterColumn<string>(
                name: "PromptItemVersion",
                table: "Senparc_PromptRange_PromptResult",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChatSystemPrompt",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedResultsJson",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenSelectionBiases",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
