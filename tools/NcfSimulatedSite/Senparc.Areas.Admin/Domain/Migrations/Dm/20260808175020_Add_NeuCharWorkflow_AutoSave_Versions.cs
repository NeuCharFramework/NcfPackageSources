using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_NeuCharWorkflow_AutoSave_Versions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoSaveMinutes",
                table: "ADMIN_NeuCharWorkflow",
                type: "INT",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharWorkflowVersion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    WorkflowId = table.Column<int>(type: "INT", nullable: false),
                    Revision = table.Column<int>(type: "INT", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    GraphJson = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Enabled = table.Column<bool>(type: "BIT", nullable: false),
                    TriggerType = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: true),
                    TriggerConfigJson = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    AutoSaveMinutes = table.Column<int>(type: "INT", nullable: false),
                    AdminUserId = table.Column<int>(type: "INT", nullable: false),
                    SaveSource = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharWorkflowVersion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharWorkflowVersion_WorkflowId_Revision",
                table: "ADMIN_NeuCharWorkflowVersion",
                columns: new[] { "WorkflowId", "Revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharWorkflowVersion");

            migrationBuilder.DropColumn(
                name: "AutoSaveMinutes",
                table: "ADMIN_NeuCharWorkflow");
        }
    }
}
