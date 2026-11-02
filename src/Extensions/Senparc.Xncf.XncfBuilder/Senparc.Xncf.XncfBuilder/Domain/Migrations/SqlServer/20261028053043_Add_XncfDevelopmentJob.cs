/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261028053043_Add_XncfDevelopmentJob.cs
    文件功能描述：20261028053043_Add_XncfDevelopmentJob 相关定义与实现


    创建标识：Senparc - 20261031

    修改标识：Senparc - 20261103
    修改描述：v0.41.7 支持 Add_XncfDevelopmentJob 数据库迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.XncfBuilder.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_XncfDevelopmentJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XncfBuilderXncfDevelopmentJob",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OwnerAdminUserId = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    ModuleProjectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetSolutionFilePath = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    WorkspaceRootPath = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    WorkspaceSolutionFilePath = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    Requirement = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetModuleFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    WorkspaceModuleFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ValidationSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiffSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviewSessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SandboxSessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PreviewUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MergeRequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfBuilderXncfDevelopmentJob", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XncfBuilderXncfDevelopmentJob_JobId",
                table: "XncfBuilderXncfDevelopmentJob",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XncfBuilderXncfDevelopmentJob_ModuleProjectName_CreatedAtUtc",
                table: "XncfBuilderXncfDevelopmentJob",
                columns: new[] { "ModuleProjectName", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_XncfBuilderXncfDevelopmentJob_OwnerAdminUserId_UpdatedAtUtc",
                table: "XncfBuilderXncfDevelopmentJob",
                columns: new[] { "OwnerAdminUserId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_XncfBuilderXncfDevelopmentJob_Stage_UpdatedAtUtc",
                table: "XncfBuilderXncfDevelopmentJob",
                columns: new[] { "Stage", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XncfBuilderXncfDevelopmentJob");
        }
    }
}
