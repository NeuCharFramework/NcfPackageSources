/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260811180607_Add_A2A_PublishedAgents.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260813

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_A2A_PublishedAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_PublishedA2AAgent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    AgentTemplateId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    PublicAgentKey = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    Enable = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CardName = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: true),
                    CardDescription = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    SkillId = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    SkillName = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: true),
                    SkillDescription = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    AllowFunctionCalls = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    MaxInputCharacters = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AuthHeaderName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    AuthSecretKey = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_PublishedA2AAgent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_PublishedA2AAgent_AgentTemplateId",
                table: "Senparc_AgentsManager_PublishedA2AAgent",
                column: "AgentTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_PublishedA2AAgent_Enable",
                table: "Senparc_AgentsManager_PublishedA2AAgent",
                column: "Enable");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_PublishedA2AAgent_PublicAgentKey",
                table: "Senparc_AgentsManager_PublishedA2AAgent",
                column: "PublicAgentKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_PublishedA2AAgent");
        }
    }
}
