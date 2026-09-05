using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_TokenMonitor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_AIKernel_AITokenUsage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModelAlias = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DeploymentName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    AiPlatform = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfigModelType = table.Column<int>(type: "INTEGER", nullable: false),
                    InputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    CachedInputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    ReasoningTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ErrorNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AIKernel_AITokenUsage", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AITokenUsage");
        }
    }
}
