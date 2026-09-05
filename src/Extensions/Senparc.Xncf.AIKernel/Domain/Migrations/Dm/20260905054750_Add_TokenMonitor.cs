using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Dm
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
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    ModelAlias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    ModelId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    DeploymentName = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: true),
                    AiPlatform = table.Column<int>(type: "INT", nullable: false),
                    ConfigModelType = table.Column<int>(type: "INT", nullable: false),
                    InputTokens = table.Column<long>(type: "BIGINT", nullable: false),
                    OutputTokens = table.Column<long>(type: "BIGINT", nullable: false),
                    TotalTokens = table.Column<long>(type: "BIGINT", nullable: false),
                    CachedInputTokens = table.Column<long>(type: "BIGINT", nullable: false),
                    ReasoningTokens = table.Column<long>(type: "BIGINT", nullable: false),
                    DurationMs = table.Column<int>(type: "INT", nullable: false),
                    Status = table.Column<int>(type: "INT", nullable: false),
                    Source = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    ErrorNote = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
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
