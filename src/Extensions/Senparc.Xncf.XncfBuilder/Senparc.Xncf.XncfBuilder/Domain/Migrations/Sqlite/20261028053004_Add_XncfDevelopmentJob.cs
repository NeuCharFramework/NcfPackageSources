/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261028053004_Add_XncfDevelopmentJob.cs
    文件功能描述：20261028053004_Add_XncfDevelopmentJob 相关定义与实现


    创建标识：Senparc - 20261031

    修改标识：Senparc - 20261103
    修改描述：v0.41.7 支持 Add_XncfDevelopmentJob 数据库迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.XncfBuilder.Domain.Migrations.Sqlite
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OwnerAdminUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    ModuleProjectName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    TargetSolutionFilePath = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    WorkspaceRootPath = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    WorkspaceSolutionFilePath = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    Requirement = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    TargetModuleFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    WorkspaceModuleFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ValidationSummary = table.Column<string>(type: "TEXT", nullable: true),
                    DiffSummary = table.Column<string>(type: "TEXT", nullable: true),
                    PreviewSessionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SandboxSessionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    PreviewUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MergeRequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AppliedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
