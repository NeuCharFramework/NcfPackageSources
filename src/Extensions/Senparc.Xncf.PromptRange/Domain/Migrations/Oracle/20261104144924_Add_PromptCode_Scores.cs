/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261104144924_Add_PromptCode_Scores.cs
    文件功能描述：PromptCode 评分数据库迁移


    创建标识：Senparc - 20261104

    修改标识：Senparc - 20261105
    修改描述：v0.20.14 增加 PromptItem 的 PromptCode 评分字段并更新数据库迁移

----------------------------------------------------------------*/
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Oracle
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
                type: "DECIMAL(18, 2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HumanScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "DECIMAL(18, 2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RobotScore",
                table: "Senparc_PromptRange_PromptItem",
                type: "DECIMAL(18, 2)",
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
