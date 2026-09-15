/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260905054730_Add_TokenMonitor.cs
    文件功能描述：20260905054730_Add_TokenMonitor 相关功能实现


    创建标识：Senparc - 20240105

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

----------------------------------------------------------------*/
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Oracle
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ModelAlias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    ModelId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    DeploymentName = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: true),
                    AiPlatform = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ConfigModelType = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    InputTokens = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    OutputTokens = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TotalTokens = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    CachedInputTokens = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    ReasoningTokens = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    DurationMs = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Source = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    ErrorNote = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
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
