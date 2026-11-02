/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261028053041_Add_XncfDevelopmentJob.cs
    文件功能描述：20261028053041_Add_XncfDevelopmentJob 相关定义与实现


    创建标识：Senparc - 20261031

    修改标识：Senparc - 20261103
    修改描述：v0.41.7 支持 Add_XncfDevelopmentJob 数据库迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.XncfBuilder.Domain.Migrations.MySql
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
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    JobId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerAdminUserId = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    ModuleProjectName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetSolutionFilePath = table.Column<string>(type: "varchar(1200)", maxLength: 1200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkspaceRootPath = table.Column<string>(type: "varchar(1200)", maxLength: 1200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkspaceSolutionFilePath = table.Column<string>(type: "varchar(1200)", maxLength: 1200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Requirement = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    StatusMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetModuleFingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkspaceModuleFingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidationSummary = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiffSummary = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviewSessionId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SandboxSessionId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviewUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MergeRequestedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfBuilderXncfDevelopmentJob", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
