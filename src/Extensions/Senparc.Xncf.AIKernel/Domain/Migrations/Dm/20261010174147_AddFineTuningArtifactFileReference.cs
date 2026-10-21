/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20261010174147_AddFineTuningArtifactFileReference.cs
    文件功能描述：EF Core 数据库迁移
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 增加微调产物文件引用数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class AddFineTuningArtifactFileReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentSha256",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "NVARCHAR2(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "INT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Senparc_AIKernel_AIFineTuningArtifact",
                type: "NVARCHAR2(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentSha256",
                table: "Senparc_AIKernel_AIFineTuningArtifact");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Senparc_AIKernel_AIFineTuningArtifact");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "Senparc_AIKernel_AIFineTuningArtifact");
        }
    }
}
