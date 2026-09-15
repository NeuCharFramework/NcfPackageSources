/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260905055158_Add_TokenMonitor.cs
    文件功能描述：20260905055158_Add_TokenMonitor 相关功能实现


    创建标识：Senparc - 20211211

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

----------------------------------------------------------------*/
using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.MySql
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ModelAlias = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModelId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeploymentName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AiPlatform = table.Column<int>(type: "int", nullable: false),
                    ConfigModelType = table.Column<int>(type: "int", nullable: false),
                    InputTokens = table.Column<long>(type: "bigint", nullable: false),
                    OutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    TotalTokens = table.Column<long>(type: "bigint", nullable: false),
                    CachedInputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ReasoningTokens = table.Column<long>(type: "bigint", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorNote = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_Senparc_AIKernel_AITokenUsage", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AITokenUsage");
        }
    }
}
