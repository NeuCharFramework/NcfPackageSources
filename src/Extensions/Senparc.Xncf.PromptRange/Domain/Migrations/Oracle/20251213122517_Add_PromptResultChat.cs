using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_PromptResultChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "Senparc_PromptRange_PromptResult",
                type: "NUMBER(10)",
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

            migrationBuilder.AddColumn<bool>(
                name: "IsAIGrade",
                table: "Senparc_PromptRange_PromptItem",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptResultChat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PromptResultId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    RoleType = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Content = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Sequence = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    UserFeedback = table.Column<bool>(type: "NUMBER(1)", nullable: true),
                    UserScore = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptResultChat", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_PromptRange_PromptResultChat");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Senparc_PromptRange_PromptResult");

            migrationBuilder.DropColumn(
                name: "IsAIGrade",
                table: "Senparc_PromptRange_PromptItem");

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
