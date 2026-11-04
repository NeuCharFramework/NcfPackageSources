using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_PromptCode_Scores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FinalScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "DECIMAL(29,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HumanScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "DECIMAL(29,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RobotScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "DECIMAL(29,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropColumn(
                name: "HumanScore",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropColumn(
                name: "RobotScore",
                table: "Senparc_PromptRange_PromptItem");
        }
    }
}
