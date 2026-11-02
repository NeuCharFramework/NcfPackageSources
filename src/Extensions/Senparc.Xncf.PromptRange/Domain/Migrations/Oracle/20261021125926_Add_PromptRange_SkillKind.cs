/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261021125926_Add_PromptRange_SkillKind.cs
    文件功能描述：20261021125926_Add_PromptRange_SkillKind 相关定义与实现


    创建标识：Senparc - 20251213

    修改标识：Senparc - 20261103
    修改描述：v0.20.8 支持 Add_PromptRange_SkillKind 数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_PromptRange_SkillKind : Migration
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

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Senparc_PromptRange_PromptRange",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

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
                name: "Kind",
                table: "Senparc_PromptRange_PromptRange");

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
