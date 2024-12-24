using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class UseDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "RobotScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "HumanScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "FinalScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "FullVersion",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "EvalMaxScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "decimal(18,2)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "EvalAvgScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "decimal(18,2)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RobotScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "HumanScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "FinalScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "FullVersion",
                table: "Senparc_PromptRange_PromptItem",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "EvalMaxScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "int",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<int>(
                name: "EvalAvgScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "int",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldMaxLength: 3);
        }
    }
}
