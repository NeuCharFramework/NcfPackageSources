/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260802175643_Add_FooterContent.cs
    文件功能描述：实现 Entity Framework 数据库迁移。


    创建标识：Senparc - 20210105

    修改标识：Senparc - 20260804
    修改描述：v0.15.3-preview6 补齐 SystemConfig.FooterContent 数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.SystemManager.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_FooterContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FooterContent",
                table: "SystemConfigs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "© 2026 Senparc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FooterContent",
                table: "SystemConfigs");
        }
    }
}
