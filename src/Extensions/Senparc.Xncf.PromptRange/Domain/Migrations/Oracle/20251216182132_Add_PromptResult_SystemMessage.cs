using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_PromptResult_SystemMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "UserScore",
                table: "Senparc_PromptRange_PromptResultChat",
                type: "DECIMAL(18, 2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RobotScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "DECIMAL(18, 2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HumanScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "DECIMAL(18, 2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FinalScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "DECIMAL(18, 2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "SystemMessage",
                table: "Senparc_PromptRange_PromptResult",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "EvalMaxScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "DECIMAL(18, 2)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,2)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "EvalAvgScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "DECIMAL(18, 2)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18,2)",
                oldMaxLength: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemMessage",
                table: "Senparc_PromptRange_PromptResult");

            migrationBuilder.AlterColumn<decimal>(
                name: "UserScore",
                table: "Senparc_PromptRange_PromptResultChat",
                type: "DECIMAL(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18, 2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RobotScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "DECIMAL(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18, 2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HumanScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "DECIMAL(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18, 2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FinalScore",
                table: "Senparc_PromptRange_PromptResult",
                type: "DECIMAL(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18, 2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "EvalMaxScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "DECIMAL(18,2)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18, 2)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "EvalAvgScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "DECIMAL(18,2)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(18, 2)",
                oldMaxLength: 3);
        }
    }
}
