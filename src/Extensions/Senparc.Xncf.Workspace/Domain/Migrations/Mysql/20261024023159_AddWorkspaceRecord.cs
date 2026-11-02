/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261024023159_AddWorkspaceRecord.cs
    文件功能描述：20261024023159_AddWorkspaceRecord 相关定义与实现


    创建标识：Senparc - 20211211

    修改标识：Senparc - 20261103
    修改描述：v0.2.2 支持 AddWorkspaceRecord 数据库迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Workspace.Domain.Migrations.Mysql
{
    /// <inheritdoc />
    public partial class AddWorkspaceRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Senparc_Workspace_WorkspaceRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WorkspaceId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhysicalPath = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    OwnerType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentTemplateId = table.Column<int>(type: "int", nullable: true),
                    StorageProvider = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_Senparc_Workspace_WorkspaceRecord", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_Workspace_WorkspaceRecord");
        }
    }
}
