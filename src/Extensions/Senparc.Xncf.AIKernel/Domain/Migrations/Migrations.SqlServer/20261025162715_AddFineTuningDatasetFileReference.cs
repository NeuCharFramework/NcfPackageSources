/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261025162715_AddFineTuningDatasetFileReference.cs
    文件功能描述：20261025162715_AddFineTuningDatasetFileReference 相关定义与实现


    创建标识：Senparc - 20210105

    修改标识：Senparc - 20261103
    修改描述：v0.17.0 支持 AddFineTuningDatasetFileReference 数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddFineTuningDatasetFileReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningDataset",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningDataset");
        }
    }
}
