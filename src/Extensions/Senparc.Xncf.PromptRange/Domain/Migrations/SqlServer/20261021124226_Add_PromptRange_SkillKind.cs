/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261021124226_Add_PromptRange_SkillKind.cs
    文件功能描述：20261021124226_Add_PromptRange_SkillKind 相关定义与实现


    创建标识：Senparc - 20210105

    修改标识：Senparc - 20261103
    修改描述：v0.20.8 支持 Add_PromptRange_SkillKind 数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_PromptRange_SkillKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Senparc_PromptRange_PromptRange",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Senparc_PromptRange_PromptRange");
        }
    }
}
