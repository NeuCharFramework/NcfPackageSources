/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260802164259_Add_XncfPreview.cs
    文件功能描述：实现 Entity Framework 数据库迁移。


    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.XncfBuilder.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_XncfPreview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XncfBuilderXncfPreviewHost",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ModuleProjectName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProcessId = table.Column<int>(type: "integer", nullable: false),
                    EnvironmentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublishDirectory = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessStartedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    HealthyAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StoppedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExitCode = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfBuilderXncfPreviewHost", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XncfBuilderXncfPreviewTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ModuleProjectName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SolutionFilePath = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    StatusMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    SourceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ModuleAssemblySha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RecentOutput = table.Column<string>(type: "text", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfBuilderXncfPreviewTask", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XncfBuilderXncfPreviewHost_SessionId",
                table: "XncfBuilderXncfPreviewHost",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XncfBuilderXncfPreviewHost_Status_UpdatedAtUtc",
                table: "XncfBuilderXncfPreviewHost",
                columns: new[] { "Status", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_XncfBuilderXncfPreviewTask_ModuleProjectName_StartedAtUtc",
                table: "XncfBuilderXncfPreviewTask",
                columns: new[] { "ModuleProjectName", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_XncfBuilderXncfPreviewTask_SessionId",
                table: "XncfBuilderXncfPreviewTask",
                column: "SessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XncfBuilderXncfPreviewHost");

            migrationBuilder.DropTable(
                name: "XncfBuilderXncfPreviewTask");
        }
    }
}
