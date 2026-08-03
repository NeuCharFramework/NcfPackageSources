using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.XncfBuilder.Domain.Migrations.Dm
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
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    ModuleProjectName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: false),
                    Url = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    ProcessId = table.Column<int>(type: "INT", nullable: false),
                    EnvironmentName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    PublishDirectory = table.Column<string>(type: "NVARCHAR2(1200)", maxLength: 1200, nullable: true),
                    Status = table.Column<int>(type: "INT", nullable: false),
                    StatusMessage = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    ProcessStartedAtUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    HealthyAtUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    StoppedAtUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    ExitCode = table.Column<int>(type: "INT", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfBuilderXncfPreviewHost", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XncfBuilderXncfPreviewTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    ModuleProjectName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: false),
                    SolutionFilePath = table.Column<string>(type: "NVARCHAR2(1200)", maxLength: 1200, nullable: true),
                    Stage = table.Column<int>(type: "INT", nullable: false),
                    ProgressPercent = table.Column<int>(type: "INT", nullable: false),
                    StatusMessage = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "CLOB", nullable: true),
                    SourceFingerprint = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    ModuleAssemblySha256 = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    RecentOutput = table.Column<string>(type: "CLOB", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
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
