/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261021162011_Add_PromptRange_SkillArtifactSource.cs
    文件功能描述：20261021162011_Add_PromptRange_SkillArtifactSource 相关定义与实现


    创建标识：Senparc - 20261022

    修改标识：Senparc - 20261103
    修改描述：v0.20.8 支持 Add_PromptRange_SkillArtifactSource 数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_PromptRange_SkillArtifactSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceFileId",
                table: "Senparc_PromptRange_PromptRange",
                type: "INT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Senparc_PromptRange_PromptRange",
                type: "NVARCHAR2(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Senparc_PromptRange_PromptRange",
                type: "NVARCHAR2(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceFileId",
                table: "Senparc_PromptRange_PromptRange");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Senparc_PromptRange_PromptRange");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Senparc_PromptRange_PromptRange");
        }
    }
}
