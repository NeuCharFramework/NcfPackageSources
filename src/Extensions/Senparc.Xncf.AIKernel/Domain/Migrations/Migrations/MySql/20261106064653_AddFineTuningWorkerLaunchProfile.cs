/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261106064653_AddFineTuningWorkerLaunchProfile.cs
    文件功能描述：MySql 数据库迁移 AddFineTuningWorkerLaunchProfile

    创建标识：Senparc - 20261106

    修改标识：Senparc - 20261108
    修改描述：v0.18.0 MySql 数据库迁移 AddFineTuningWorkerLaunchProfile

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddFineTuningWorkerLaunchProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LaunchProfileJson",
                table: "Senparc_AIKernel_AIFineTuningWorker",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaunchProfileJson",
                table: "Senparc_AIKernel_AIFineTuningWorker");
        }
    }
}
