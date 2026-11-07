/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261106065103_AddFineTuningWorkerLaunchProfile.cs
    文件功能描述：Dm 数据库迁移 AddFineTuningWorkerLaunchProfile

    创建标识：Senparc - 20261106

    修改标识：Senparc - 20261108
    修改描述：v0.18.0 Dm 数据库迁移 AddFineTuningWorkerLaunchProfile

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Dm
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
                type: "NVARCHAR2(2000)",
                maxLength: 2000,
                nullable: true);
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
