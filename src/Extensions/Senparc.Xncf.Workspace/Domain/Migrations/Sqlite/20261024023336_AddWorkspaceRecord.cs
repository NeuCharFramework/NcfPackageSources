/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261024023336_AddWorkspaceRecord.cs
    文件功能描述：20261024023336_AddWorkspaceRecord 相关定义与实现


    创建标识：Senparc - 20210108

    修改标识：Senparc - 20261103
    修改描述：v0.2.2 支持 AddWorkspaceRecord 数据库迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Workspace.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddWorkspaceRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_Workspace_WorkspaceRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PhysicalPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    OwnerId = table.Column<long>(type: "INTEGER", nullable: false),
                    OwnerType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    AgentTemplateId = table.Column<int>(type: "INTEGER", nullable: true),
                    StorageProvider = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_Workspace_WorkspaceRecord", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_Workspace_WorkspaceRecord");
        }
    }
}
