using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.XncfBuilder.Domain.Migrations.MySql
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModuleProjectName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    EnvironmentName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PublishDirectory = table.Column<string>(type: "varchar(1200)", maxLength: 1200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessStartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HealthyAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StoppedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExitCode = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_XncfBuilderXncfPreviewHost", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "XncfBuilderXncfPreviewTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModuleProjectName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SolutionFilePath = table.Column<string>(type: "varchar(1200)", maxLength: 1200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    StatusMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceFingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModuleAssemblySha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecentOutput = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_XncfBuilderXncfPreviewTask", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
