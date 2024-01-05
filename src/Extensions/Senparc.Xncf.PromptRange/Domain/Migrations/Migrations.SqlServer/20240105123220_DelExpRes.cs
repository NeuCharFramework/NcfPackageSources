using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class DelExpRes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedResultsJson",
                table: "Senparc_PromptRange_PromptRange");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedResultsJson",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedResultsJson",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedResultsJson",
                table: "Senparc_PromptRange_PromptRange",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
