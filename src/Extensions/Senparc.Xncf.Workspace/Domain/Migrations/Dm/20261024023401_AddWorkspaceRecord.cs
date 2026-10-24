using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Workspace.Domain.Migrations.Dm
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
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    PhysicalPath = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: false),
                    OwnerId = table.Column<long>(type: "BIGINT", nullable: false),
                    OwnerType = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    AgentTemplateId = table.Column<int>(type: "INT", nullable: true),
                    StorageProvider = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
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
